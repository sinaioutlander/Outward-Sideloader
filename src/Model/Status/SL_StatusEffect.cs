﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using SideLoader.Helpers;
using SideLoader.Model;
using SideLoader.SaveData;
using UnityEngine;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_StatusEffect : IPrefabTemplate<string>
    {
        public bool IsCreatingNewID => !string.IsNullOrEmpty(this.StatusIdentifier) && this.StatusIdentifier != this.TargetStatusIdentifier;
        public bool DoesTargetExist => ResourcesPrefabManager.Instance.GetStatusEffectPrefab(this.TargetStatusIdentifier);

        public string TargetID => this.TargetStatusIdentifier;
        public string AppliedID => this.StatusIdentifier;

        public void CreatePrefab() => this.Internal_Create();

        // ~~~~~~~~~~~~~~~~~~~~

        /// <summary>Invoked when this template is applied during SideLoader's start or hot-reload.</summary>
        public event Action<StatusEffect> OnTemplateApplied;

        /// <summary> [NOT SERIALIZED] The name of the SLPack this custom status template comes from (or is using).
        /// If defining from C#, you can set this to the name of the pack you want to load assets from.</summary>
        [XmlIgnore] public string SLPackName;
        /// <summary> [NOT SERIALIZED] The name of the folder this custom status is using for the icon.png (MyPack/StatusEffects/[SubfolderName]/icon.png).</summary>
        [XmlIgnore] public string SubfolderName;

        /// <summary> The StatusEffect you would like to clone from. Can also use TargetStatusID (checks for a Preset ID), but this takes priority.</summary>
        public string TargetStatusIdentifier;

        /// <summary>The new Preset ID for your Status Effect.</summary>
        public int NewStatusID;
        /// <summary>The new Status Identifier name for your Status Effect. Used by ResourcesPrefabManager.GetStatusEffect(string identifier)</summary>
        public string StatusIdentifier;

        public string Name;
        public string Description;

        public float? Lifespan;
        public float? RefreshRate;

        public int? Priority;

        public string AmplifiedStatusIdentifier;

        public float? BuildupRecoverySpeed;
        public bool? IgnoreBuildupIfApplied;

        public bool? DisplayedInHUD;
        public bool? IsHidden;
        public bool? IsMalusEffect;

        public string ComplicationStatusIdentifier;
        public string RequiredStatusIdentifier;
        public bool? RemoveRequiredStatus;
        public bool? NormalizeDamageDisplay;
        public bool? IgnoreBarrier;

        public StatusEffect.ActionsOnHit? ActionOnHit;

        public StatusEffect.FamilyModes? FamilyMode;
        public SL_StatusEffectFamily BindFamily;
        public string ReferenceFamilyUID;

        public string EffectTypeTag;
        public string[] Tags;

        public bool? PlayFXOnActivation;
        public Vector3? FXOffset;
        public StatusEffect.FXInstantiationTypes? VFXInstantiationType;
        public SL_PlayVFX.VFXPrefabs? VFXPrefab;
        public GlobalAudioManager.Sounds? SpecialSFX;
        public bool? PlaySpecialFXOnStop;

        public EditBehaviours EffectBehaviour = EditBehaviours.Override;
        public SL_EffectTransform[] Effects;

        [Obsolete("Use SL_StatusEffect.BindFamily or SL_StatusFamily instead")]
        [XmlIgnore] public StatusEffectFamily.LengthTypes? LengthType;

        /// <summary>
        /// Call this to apply your template at Awake or BeforePacksLoaded.
        /// </summary>
        public void Apply()
        {
            if (SL.PacksLoaded)
            {
                SL.LogWarning("Applying a template AFTER SL.OnPacksLoaded has been called. This is not recommended, use SL.BeforePacksLoaded instead.");
                Internal_Create();
            }
            else
                SL.PendingStatuses.Add(this);
        }

        internal void Internal_Create()
        {
            if (string.IsNullOrEmpty(this.StatusIdentifier))
                this.StatusIdentifier = this.TargetStatusIdentifier;

            var status = CustomStatusEffects.CreateCustomStatus(this);

            ApplyTemplate(status);
            OnTemplateApplied?.Invoke(status);
        }

        internal virtual void ApplyTemplate(StatusEffect status)
        {
            SL.Log("Applying Status Effect template: " + Name ?? status.name);

            CustomStatusEffects.SetStatusLocalization(status, Name, Description);

            if (status.StatusData == null)
                status.StatusData = new StatusData();

            if (Lifespan != null)
            {
                var data = status.StatusData;
                data.LifeSpan = (float)Lifespan;
            }

            if (RefreshRate != null)
                status.RefreshRate = (float)RefreshRate;

            if (this.Priority != null)
                At.SetField(status, "m_priority", (int)this.Priority);

            if (BuildupRecoverySpeed != null)
                status.BuildUpRecoverSpeed = (float)BuildupRecoverySpeed;

            if (IgnoreBuildupIfApplied != null)
                status.IgnoreBuildUpIfApplied = (bool)IgnoreBuildupIfApplied;

            if (DisplayedInHUD != null)
                status.DisplayInHud = (bool)DisplayedInHUD;

            if (IsHidden != null)
                status.IsHidden = (bool)IsHidden;
            
            if (IsMalusEffect != null)
                status.IsMalusEffect = (bool)this.IsMalusEffect;

            if (this.ActionOnHit != null)
                At.SetField(status, "m_actionOnHit", (StatusEffect.ActionsOnHit)this.ActionOnHit);

            if (!string.IsNullOrEmpty(this.ComplicationStatusIdentifier))
            {
                var complicStatus = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(this.ComplicationStatusIdentifier);
                if (complicStatus)
                    status.ComplicationStatus = complicStatus;
            }

            if (!string.IsNullOrEmpty(RequiredStatusIdentifier))
            {
                var required = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(this.RequiredStatusIdentifier);
                if (required)
                    status.RequiredStatus = required;
            }

            if (this.RemoveRequiredStatus != null)
                status.RemoveRequiredStatus = (bool)this.RemoveRequiredStatus;

            if (this.NormalizeDamageDisplay != null)
                status.NormalizeDamageDisplay = (bool)this.NormalizeDamageDisplay;

            if (this.IgnoreBarrier != null)
                status.IgnoreBarrier = (bool)this.IgnoreBarrier;

            if (!string.IsNullOrEmpty(this.AmplifiedStatusIdentifier))
            {
                var amp = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(AmplifiedStatusIdentifier);
                if (amp)
                    At.SetField(status, "m_amplifiedStatus", amp);
                else
                    SL.Log("StatusEffect.ApplyTemplate - could not find AmplifiedStatusIdentifier " + this.AmplifiedStatusIdentifier);
            }

            if (this.EffectTypeTag != null)
            {
                var tag = CustomTags.GetTag(this.EffectTypeTag);
                At.SetField(status, "m_effectType", tag);
            }
            else if (IsCreatingNewID)
            {
                At.SetField(status, "m_effectType", new TagSourceSelector(Tag.None));
            }

            if (Tags != null)
            {
                var tagSource = CustomTags.SetTagSource(status.gameObject, Tags, true);
                At.SetField(status, "m_tagSource", tagSource);
            }
            else if (!status.GetComponent<TagSource>())
            {
                var tagSource = status.gameObject.AddComponent<TagSource>();
                At.SetField(status, "m_tagSource", tagSource);
            }

            if (this.PlayFXOnActivation != null)
                status.PlayFXOnActivation = (bool)this.PlayFXOnActivation;

            if (this.VFXInstantiationType != null)
                status.FxInstantiation = (StatusEffect.FXInstantiationTypes)this.VFXInstantiationType;

            if (this.VFXPrefab != null)
            {
                if (this.VFXPrefab == SL_PlayVFX.VFXPrefabs.NONE)
                    status.FXPrefab = null;
                else
                {
                    var clone = GameObject.Instantiate(SL_PlayVFX.GetVfxSystem((SL_PlayVFX.VFXPrefabs)this.VFXPrefab));
                    GameObject.DontDestroyOnLoad(clone);
                    status.FXPrefab = clone.transform;
                }
            }

            if (this.FXOffset != null)
                status.FxOffset = (Vector3)this.FXOffset;

            if (this.PlaySpecialFXOnStop != null)
                status.PlaySpecialFXOnStop = (bool)this.PlaySpecialFXOnStop;

            // setup family 
            if (this.FamilyMode == null && IsCreatingNewID)
            {
                // Creating a new status, but no unique bind family was declared. Create one.
                var family = new StatusEffectFamily
                {
                    Name = this.StatusIdentifier + "_FAMILY",
                    LengthType = StatusEffectFamily.LengthTypes.Short,
                    MaxStackCount = 1,
                    StackBehavior = StatusEffectFamily.StackBehaviors.IndependantUnique
                };

                At.SetField(status, "m_bindFamily", family);
                At.SetField(status, "m_familyMode", StatusEffect.FamilyModes.Bind);
            }
            if (this.FamilyMode == StatusEffect.FamilyModes.Bind)
            {
                At.SetField(status, "m_familyMode", StatusEffect.FamilyModes.Bind);
                
                if (this.BindFamily != null)
                {
                    // set bind using SL_StatusEffectFamily template
                    At.SetField(status, "m_bindFamily", this.BindFamily.CreateFamily());
                }
            }
            else if (this.FamilyMode == StatusEffect.FamilyModes.Reference)
            {
                At.SetField(status, "m_familyMode", StatusEffect.FamilyModes.Reference);

                if (this.ReferenceFamilyUID != null)
                    At.SetField(status, "m_stackingFamily", new StatusEffectFamilySelector() { SelectorValue = this.ReferenceFamilyUID });
            }

            // setup signature and finalize

            if (EffectBehaviour == EditBehaviours.Destroy)
                UnityHelpers.DestroyChildren(status.transform);

            Transform signature;
            if (status.transform.childCount < 1)
            {
                signature = new GameObject($"SIGNATURE_{status.IdentifierName}").transform;
                signature.parent = status.transform;
                var comp = signature.gameObject.AddComponent<EffectSignature>();
                comp.SignatureUID = new UID($"{NewStatusID}_{status.IdentifierName}");
            }
            else
                signature = status.transform.GetChild(0);

            if (Effects != null)
            {
                if (signature)
                    SL_EffectTransform.ApplyTransformList(signature, Effects, EffectBehaviour);
                else
                    SL.Log("Could not get effect signature!");
            }

            // check for custom icon
            if (!string.IsNullOrEmpty(SLPackName) && !string.IsNullOrEmpty(SubfolderName) && SL.Packs[SLPackName] is SLPack pack)
            {
                var path = pack.GetSubfolderPath(SLPack.SubFolders.StatusEffects) + "\\" + SubfolderName + "\\icon.png";

                if (File.Exists(path))
                {
                    var tex = CustomTextures.LoadTexture(path, false, false);
                    var sprite = CustomTextures.CreateSprite(tex, CustomTextures.SpriteBorderTypes.NONE);

                    status.OverrideIcon = sprite;
                    //At.SetField(status, "m_defaultStatusIcon", new StatusTypeIcon(Tag.None) { Icon = sprite });
                }
            }

            // fix StatusData for the new effects
            CompileEffectsToData(status);
        }

        // Generate StatusData for the 
        public static void CompileEffectsToData(StatusEffect status)
        {
            // Get the EffectSignature component
            var signature = status.GetComponentInChildren<EffectSignature>();

            status.StatusData.EffectSignature = signature;

            // Get and Set the Effects list
            var effects = signature.GetComponentsInChildren<Effect>()?.ToList() ?? new List<Effect>();
            signature.Effects = effects;

            // Finally, set the EffectsData[] array.
            status.StatusData.EffectsData = GenerateEffectsData(effects);

            // Not sure if this is needed or not, but I'm doing it to be extra safe.
            At.SetField(status, "m_totalData", status.StatusData.EffectsData);
        }

        public static StatusData.EffectData[] GenerateEffectsData(List<Effect> effects, int level = 1)
        {
            // Create a list to generate the EffectData[] array into.
            var list = new List<StatusData.EffectData>();

            // Iterate over the effects
            foreach (var effect in effects)
            {
                // Create a blank holder, in the case this effect isn't supported or doesn't serialize anything.
                var data = new StatusData.EffectData()
                {
                    Data = new string[] { "" }
                };

                var type = effect.GetType();

                if (typeof(PunctualDamage).IsAssignableFrom(type))
                {
                    var comp = effect as PunctualDamage;

                    // For PunctualDamage, Data[0] is the entire damage, and Data[1] is the impact.

                    // Each damage goes "Damage : Type", and separated by a ';' char.
                    // So we just iterate over the damage and serialize like that.
                    var dmgString = "";
                    foreach (var dmg in comp.Damages)
                    {
                        if (dmgString != "")
                            dmgString += ";";

                        dmgString += $"{dmg.Damage * level}:{dmg.Type}";
                    }

                    // finally, set data
                    data.Data = new string[]
                    {
                        dmgString,
                        comp.Knockback.ToString()
                    };
                }
                // For most AffectStat components, the only thing that is serialized is the AffectQuantity.
                else if (type.GetField("AffectQuantity", At.FLAGS) is FieldInfo fi_AffectQuantity)
                {
                    data.Data = new string[]
                    {
                        ((float)fi_AffectQuantity.GetValue(effect) * level).ToString()
                    };
                }
                // AffectMana uses "Value" instead of AffectQuantity for some reason...
                else if (type.GetField("Value", At.FLAGS) is FieldInfo fi_Value)
                {
                    data.Data = new string[]
                    {
                        ((float)fi_Value.GetValue(effect) * level).ToString()
                    };
                }
                else // otherwise I need to add support for this effect (maybe).
                {
                    //SL.Log("[StatusEffect] Unsupported effect: " + type, 1);
                }

                list.Add(data);

            }

            return list.ToArray();
        }

        public static SL_StatusEffect ParseStatusEffect(StatusEffect status)
        {
            var type = Serializer.GetBestSLType(status.GetType());

            var template = (SL_StatusEffect)Activator.CreateInstance(type);

            template.SerializeStatus(status);

            return template;
        }

        public virtual void SerializeStatus(StatusEffect status) 
        {
            var preset = status.GetComponent<EffectPreset>();

            this.NewStatusID = preset?.PresetID ?? -1;
            this.TargetStatusIdentifier = status.IdentifierName;
            this.StatusIdentifier = status.IdentifierName;
            this.IgnoreBuildupIfApplied = status.IgnoreBuildUpIfApplied;
            this.BuildupRecoverySpeed = status.BuildUpRecoverSpeed;
            this.DisplayedInHUD = status.DisplayInHud;
            this.IsHidden = status.IsHidden;
            this.Lifespan = status.StatusData.LifeSpan;
            this.RefreshRate = status.RefreshRate;
            this.AmplifiedStatusIdentifier = status.AmplifiedStatus?.IdentifierName ?? "";
            this.PlayFXOnActivation = status.PlayFXOnActivation;
            this.ComplicationStatusIdentifier = status.ComplicationStatus?.IdentifierName;
            this.FXOffset = status.FxOffset;
            this.IgnoreBarrier = status.IgnoreBarrier;
            this.IsMalusEffect = status.IsMalusEffect;
            this.NormalizeDamageDisplay = status.NormalizeDamageDisplay;
            this.PlaySpecialFXOnStop = status.PlaySpecialFXOnStop;
            this.RemoveRequiredStatus = status.RemoveRequiredStatus;
            this.RequiredStatusIdentifier = status.RequiredStatus?.IdentifierName;
            this.SpecialSFX = status.SpecialSFX;
            this.VFXInstantiationType = status.FxInstantiation;

            this.Priority = (int)At.GetField(status, "m_priority");

            CustomStatusEffects.GetStatusLocalization(status, out Name, out Description);

            var tags = At.GetField(status, "m_tagSource") as TagListSelectorComponent;
            if (tags)
                Tags = tags.Tags.Select(it => it.TagName).ToArray();

            var vfx = status.FXPrefab?.GetComponent<VFXSystem>();
            if (vfx)
                VFXPrefab = SL_PlayVFX.GetVFXSystemEnum(vfx);

            // PARSE EFFECT FAMILY
            FamilyMode = status.FamilyMode;
            if (status.EffectFamily != null)
            {
                if (FamilyMode == StatusEffect.FamilyModes.Bind)
                    BindFamily = SL_StatusEffectFamily.ParseEffectFamily(status.EffectFamily);
                else
                    ReferenceFamilyUID = status.EffectFamily.UID;
            }

            // For existing StatusEffects, the StatusData contains the real values, so we need to SetValue to each Effect.
            var statusData = status.StatusData.EffectsData;
            var components = status.GetComponentsInChildren<Effect>();
            for (int i = 0; i < components.Length; i++)
            {
                var comp = components[i];
                if (comp && comp.Signature.Length > 0)
                    comp.SetValue(statusData[i].Data);
            }

            var effects = new List<SL_EffectTransform>();
            var signature = status.transform.GetChild(0);
            if (signature)
            {
                foreach (Transform child in signature.transform)
                {
                    var effectsChild = SL_EffectTransform.ParseTransform(child);

                    if (effectsChild.HasContent)
                        effects.Add(effectsChild);
                }
            }

            Effects = effects.ToArray();
        }
    }
}