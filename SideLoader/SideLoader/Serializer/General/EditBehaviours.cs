﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SideLoader
{
    /// <summary>Determines how SideLoader applies your template to the original object.</summary>
    public enum EditBehaviours
    {
        /// <summary>Will leave the existing objects untouched, and add yours on-top of them (if any).</summary>
        NONE,
        /// <summary>Will override the existing objects if you have defined an equivalent (for SL_EffectTransform, this means the SL_EffectTransform itself)</summary>
        Override,
        /// <summary>Destroys all existing objects before adding yours (if any).</summary>
        Destroy,

        // Obsolete support
        [Obsolete("Use 'Override' instead.")] OverrideEffects,
        [Obsolete("Use 'Destroy' instead.")] DestroyEffects,
    }
}
