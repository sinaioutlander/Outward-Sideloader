﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

namespace SideLoader
{
    public class SLPack
    {
        public string Name { get; private set; }

        public string FolderPath 
        { 
            get 
            {
                if (InMainSLFolder)
                    return SL.SL_FOLDER + @"\" + Name;
                else
                    return SL.PLUGINS_FOLDER + @"\" + Name + @"\SideLoader";
            } 
        }

        public bool InMainSLFolder = false;

        public Dictionary<string, AssetBundle> AssetBundles = new Dictionary<string, AssetBundle>();
        public Dictionary<string, Texture2D> Texture2D = new Dictionary<string, Texture2D>();
        public Dictionary<string, AudioClip> AudioClips = new Dictionary<string, AudioClip>();

        public enum SubFolders
        {
            AudioClip,
            AssetBundles,
            Items,
            Recipes,
            Texture2D,
        }

        /// <summary>
        /// Returns the full (relative to the Outward folder) path for the specified subfolder, for this SLPack. Eg, "Mods/SideLoader/SLPACKNAME/SubFolder"
        /// </summary>
        /// <param name="subFolder">The SubFolder you want the path for</param>
        public string GetSubfolderPath(SubFolders subFolder)
        {
            return this.FolderPath + @"\" + subFolder;
        }

        /// <summary>
        /// Loads all the assets from the specified SLPack name. Not for calling directly, just place your pack in the SideLoader folder and use SL.Packs["Folder"]
        /// </summary>
        /// <param name="name">The name of the SideLoader pack (ie. the name of the folder inside Mods/SideLoader/)</param>
        /// <param name="inMainSLFolder">Is the SLPack in Mods\SideLoader? If not, it should be Mods\ModName\SideLoader\ structure.</param>
        public static SLPack LoadFromFolder(string name, bool inMainSLFolder = true)
        {
            var pack = new SLPack()
            {
                Name = name,
                InMainSLFolder = inMainSLFolder
            };

            SL.Log("Reading SLPack " + pack.Name);

            // AssetBundles
            pack.LoadAssetBundles();

            // Audio Clips
            pack.LoadAudioClips();

            // Texture2D (Just for replacements)
            pack.LoadTexture2D();

            // Custom Items
            pack.LoadCustomItems();

            // Custom Recipes
            pack.LoadRecipes();

            return pack;
        }

        private void LoadAssetBundles()
        {
            var dir = GetSubfolderPath(SubFolders.AssetBundles);
            if (!Directory.Exists(dir))
            {
                return;
            }

            foreach (var bundlePath in Directory.GetFiles(GetSubfolderPath(SubFolders.AssetBundles))
                .Where(x => !x.EndsWith(".meta") && !x.EndsWith(".manifest")))
            {
                try
                {
                    var bundle = SL.LoadAssetBundle(bundlePath);
                    if (bundle is AssetBundle)
                    {
                        string name = Path.GetFileName(bundlePath);
                        AssetBundles.Add(name, bundle);
                        SL.Log("Loaded assetbundle " + name);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unknown error (Bundle '{0}' was null)", Path.GetFileName(bundlePath)));
                    }
                }
                catch (Exception e)
                {
                    SL.Log("Error loading asset bundle! Message: " + e.Message + "\r\nStack: " + e.StackTrace, 1);
                }
            }
        }

        private void LoadAudioClips()
        {
            var dir = GetSubfolderPath(SubFolders.AudioClip);
            if (!Directory.Exists(dir))
            {
                return;
            }

            foreach (var clipPath in Directory.GetFiles(dir, "*.wav"))
            {
                SL.Instance.StartCoroutine(CustomAudio.LoadClip(clipPath, this));
            }
        }

        // Note: Does NOT load Pngs from the CustomItems/*/Textures/ folders
        // That is done on CustomItem.ApplyTemplateToItem, those textures are not stored in the dictionary.
        private void LoadTexture2D()
        {
            if (!Directory.Exists(GetSubfolderPath(SubFolders.Texture2D)))
            {
                return;
            }

            foreach (var texPath in Directory.GetFiles(GetSubfolderPath(SubFolders.Texture2D)))
            {
                var texture = CustomTextures.LoadTexture(texPath, false, false);
                var name = Path.GetFileNameWithoutExtension(texPath);

                // add to the Texture2D dict for this pack
                Texture2D.Add(name, texture);

                // add to the global Tex replacements dict
                if (CustomTextures.Textures.ContainsKey(name))
                {
                    SL.Log("CustomTextures: A Texture already exists in the global list called " + name + "! Overwriting with this one...");
                    CustomTextures.Textures[name] = texture;
                }
                else
                {
                    CustomTextures.Textures.Add(name, texture);
                }
            }
        }

        private void LoadCustomItems()
        {
            var itemsfolder = GetSubfolderPath(SubFolders.Items);

            if (Directory.Exists(itemsfolder))
            {
                // ******** Build the list of template xml paths ******** //

                // Key: full Template filepath, Value:SubFolder name (if any)
                var templates = new Dictionary<string, string>(); 

                // get basic xml templates in the Items folder
                foreach (var file in Directory.GetFiles(itemsfolder, "*.xml"))
                {
                    templates.Add(file, "");
                }

                // check for subfolders (items which are using custom texture pngs)
                foreach (var folder in Directory.GetDirectories(itemsfolder))
                {
                    //SL.Log("Parsing CustomItem subfolder: " + Path.GetFileName(folder));

                    foreach (string path in Directory.GetFiles(folder))
                    {
                        templates.Add(path, Path.GetFileName(folder));
                    }
                }

                // ******** Serialize and prepare each template (does not apply the template, but does clone the base prefab) ******** //

                foreach (var entry in templates)
                {
                    try
                    {
                        // load the ItemHolder template and set the pack/folder info
                        var holder = Serializer.LoadFromXml(entry.Key);

                        var list = new List<SL_Item>();

                        if (holder is SL_Item)
                        {
                            list.Add(holder as SL_Item);
                        }
                        else
                        {
                            list.AddRange((holder as SL_MultiItem).Items);
                        }

                        foreach (var itemHolder in list)
                        {
                            itemHolder.SubfolderName = entry.Value;
                            itemHolder.SLPackName = Name;

                            // Clone the target item (and set it to ResourcesPrefabManager dictionary)
                            var item = CustomItems.CreateCustomItem(itemHolder.Target_ItemID, itemHolder.New_ItemID, itemHolder.Name);

                            if (!SL.PacksLoaded)
                            {
                                if (item is RecipeItem)
                                {
                                    // Add the callback for when RecipeItems are ready to be applied (after recipes)
                                    SL.INTERNAL_ApplyRecipeItems += itemHolder.ApplyTemplateToItem;
                                }
                                else
                                {
                                    // Add the callback for when Items are ready to be applied
                                    SL.INTERNAL_ApplyItems += itemHolder.ApplyTemplateToItem;
                                }
                            }
                            else
                            {
                                itemHolder.ApplyTemplateToItem();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        SL.Log("LoadFromFolder: Error creating custom item! \r\nMessage: " + e.Message + "\r\nStack: " + e.StackTrace);
                    }
                }
            }
        }

        private void LoadRecipes()
        {
            if (!Directory.Exists(GetSubfolderPath(SubFolders.Recipes)))
            {
                return;
            }

            foreach (var recipePath in Directory.GetFiles(GetSubfolderPath(SubFolders.Recipes)))
            {
                if (Serializer.LoadFromXml(recipePath) is SL_Recipe recipeHolder)
                {
                    SL.INTERNAL_ApplyRecipes += recipeHolder.ApplyRecipe;
                }
            }
        }

        // legacy support

        [Obsolete("Use SLPack.FolderPath instead.")]
        public string FullDirectory { get => FolderPath; }
    }
}
