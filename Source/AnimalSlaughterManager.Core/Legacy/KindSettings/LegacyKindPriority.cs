#pragma warning disable CS0612, CS0618

using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>
/// The v0 save format of the kind priority settings (flat condition lists), read through one
/// seam: KindPrioritySettings.ExposeData calls ReadRuleSets while loading an old save. Delete
/// together with the rest of the Legacy folder when the save-compatibility horizon allows.
/// </summary>
[Obsolete]
public static class LegacyKindPriority
{
    /// <summary>The v0 rule lists, converted; must be called in the Scribe loading phase.</summary>
    public static Dictionary<(bool Male, bool Adult), List<BasePriorityRule>> ReadRuleSets()
    {
        var legacy = new KindPrioritySettingsLegacy();
        legacy.ExposeData();

        return new Dictionary<(bool Male, bool Adult), List<BasePriorityRule>>
        {
            [(true, true)] = legacy.PriorityAdultMale,
            [(true, false)] = legacy.PriorityYoungMale,
            [(false, true)] = legacy.PriorityAdultFemale,
            [(false, false)] = legacy.PriorityYoungFemale
        };
    }
}
