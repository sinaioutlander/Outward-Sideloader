﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SideLoader
{
    public class SL_DualMeleeWeapon : SL_MeleeWeapon
    {
        // Contains no extra fields.

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);
        }

        public override void SerializeItem(Item item, SL_Item holder)
        {
            base.SerializeItem(item, holder);
        }
    }
}