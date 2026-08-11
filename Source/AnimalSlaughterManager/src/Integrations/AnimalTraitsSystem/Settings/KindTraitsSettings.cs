using System;
using System.Collections.Generic;
using Verse;

namespace ASM;

public class KindTraitsSettings
{
    // Breeding: protect these animals from slaughter.
    public List<TraitProtectRule> keepTraits = [];
    // Force slaughter: cull these animals regardless of count/limits and other settings
    // (protection still wins at intersections).
    public List<TraitRule> forceCullTraits = [];
    // TODO: obsolete?
    [Obsolete]
    // Slaughter priority: cull these animals first.
    public List<TraitRule> cullTraits = [];
    // TODO: obsolete?
    [Obsolete]
    // Slaughter priority: cull these animals LAST (less priority for slaughter).
    public List<TraitRule> spareTraits = [];

    /// <summary>True when this kind deviates from vanilla behaviour and must be recomputed.</summary>
    public bool Customized =>
        keepTraits != null && keepTraits.Count > 0 ||
        forceCullTraits != null && forceCullTraits.Count > 0 ||
        cullTraits != null && cullTraits.Count > 0 ||
        spareTraits != null && spareTraits.Count > 0;

    /// <summary>Priority customization: sex×age (older/younger) prefs, condition lists, or cull/spare trait lists.</summary>
    public bool HasPrioritySettings =>
        cullTraits != null && cullTraits.Count > 0 ||
        spareTraits != null && spareTraits.Count > 0;

    /// <summary>Protection customization: breeding ("keep") trait targets (protect from slaughter).</summary>
    public bool HasProtectionSettings => keepTraits != null && keepTraits.Count > 0;

    /// <summary>Force-slaughter customization: cull matching animals regardless of count/limits.</summary>
    public bool HasForceCullSettings => forceCullTraits != null && forceCullTraits.Count > 0;

    public void Reset()
    {
        keepTraits.Clear();
        forceCullTraits.Clear();
        cullTraits.Clear();
        spareTraits.Clear();
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref keepTraits, "keepTraits", LookMode.Deep);
        Scribe_Collections.Look(ref forceCullTraits, "forceCullTraits", LookMode.Deep);
        Scribe_Collections.Look(ref cullTraits, "cullTraits", LookMode.Deep);
        Scribe_Collections.Look(ref spareTraits, "spareTraits", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            keepTraits ??= [];
            forceCullTraits ??= [];
            cullTraits ??= [];
            spareTraits ??= [];

            // A trait/disease/trainable def may resolve to null when the mod that defined it
            // (e.g. Animal Traits System) was disabled on this save. Drop those dead entries so
            // the settings don't fill up with no-op "?" rows. (This does not silence RimWorld's
            // own "Could not load reference" log for hediffs still on the pawns — that is the
            // base game resolving the save, outside this mod's control.)
            keepTraits.RemoveAll(t => t == null || t.trait == null);
            forceCullTraits.RemoveAll(t => t == null || t.trait == null);
            cullTraits.RemoveAll(t => t == null || t.trait == null);
            spareTraits.RemoveAll(t => t == null || t.trait == null);
        }
    }
}
