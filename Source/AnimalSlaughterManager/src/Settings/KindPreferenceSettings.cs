using System;
using Verse;

namespace ASM;

public class KindPreferenceSettings
{
    public SlaughterPreference malePref = SlaughterPreference.OldestFirst;
    public SlaughterPreference maleYoungPref = SlaughterPreference.OldestFirst;
    public SlaughterPreference femalePref = SlaughterPreference.OldestFirst;
    public SlaughterPreference femaleYoungPref = SlaughterPreference.OldestFirst;
    [Obsolete]
    public SlaughterPreference defaultAgePref = SlaughterPreference.OldestFirst;

    /// <summary>True when this kind deviates from vanilla behaviour and must be recomputed.</summary>
    public bool Customized =>
        malePref is SlaughterPreference.YoungestFirst ||
        femalePref is SlaughterPreference.YoungestFirst ||
        maleYoungPref is SlaughterPreference.YoungestFirst ||
        femaleYoungPref is SlaughterPreference.YoungestFirst ||
        defaultAgePref is SlaughterPreference.YoungestFirst;

    public void Reset()
    {
        // TODO: use global preferences
        malePref = femalePref = maleYoungPref = femaleYoungPref = SlaughterPreference.OldestFirst;
        defaultAgePref = SlaughterPreference.OldestFirst;
    }

    public SlaughterPreference GetPref(bool male, bool adult) =>
        male ? (adult ? malePref : maleYoungPref) : (adult ? femalePref : femaleYoungPref);

    public void SetPref(bool male, bool adult, SlaughterPreference p)
    {
        if (male)
        {
            if (adult)
            {
                malePref = p;
            }
            else
            {
                maleYoungPref = p;
            }
        }
        else
        {
            if (adult)
            {
                femalePref = p;
            }
            else
            {
                femaleYoungPref = p;
            }
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref malePref, "malePref", SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref femalePref, "femalePref", SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref maleYoungPref, "maleYoungPref", SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref femaleYoungPref, "femaleYoungPref", SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref defaultAgePref, "defaultAgePref", SlaughterPreference.OldestFirst);
    }

    /// <summary>
    /// Serializes the map-wide (global) preference settings under their legacy flat keys
    /// ("globalMalePref", …) directly in the parent node — the exact keys older saves used,
    /// so existing saves load unchanged (no migration needed). Called manually (delegated),
    /// like KindSettings does for its sub-settings, so the keys stay at the same XML level.
    /// </summary>
    public void ExposeGlobalData()
    {
        Scribe_Values.Look(ref malePref, "globalMalePref", SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref femalePref, "globalFemalePref", SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref maleYoungPref, "globalMaleYoungPref", SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref femaleYoungPref, "globalFemaleYoungPref", SlaughterPreference.OldestFirst);
    }
}
