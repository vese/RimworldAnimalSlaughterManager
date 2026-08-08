using System.Collections.Generic;
using Verse;

namespace ASM
{
    /// <summary>
    /// Per-animal-kind slaughter settings, layered on top of vanilla AutoSlaughterConfig.
    /// Slaughter priority is split into four sex×age buckets (matching vanilla's buckets).
    /// </summary>
    public class KindSettings : IExposable
    {
        public SlaughterPreference malePref = SlaughterPreference.OldestFirst;
        public SlaughterPreference femalePref = SlaughterPreference.OldestFirst;
        public SlaughterPreference maleYoungPref = SlaughterPreference.OldestFirst;
        public SlaughterPreference femaleYoungPref = SlaughterPreference.OldestFirst;

        // Breeding: protect these animals from slaughter.
        public List<TraitTarget> keepTraits = new List<TraitTarget>();
        // Slaughter priority: cull these animals first.
        public List<CullTrait> cullTraits = new List<CullTrait>();
        // Slaughter priority: cull these animals LAST (less priority for slaughter).
        public List<CullTrait> spareTraits = new List<CullTrait>();
        // Force slaughter: cull these animals regardless of count/limits and other settings
        // (protection still wins at intersections).
        public List<CullTrait> forceCullTraits = new List<CullTrait>();

        // Per-bucket ordered slaughter-priority condition lists (top = keep, bottom = cull).
        public List<SlaughterCondition> prioAdultMale = new List<SlaughterCondition>();
        public List<SlaughterCondition> prioYoungMale = new List<SlaughterCondition>();
        public List<SlaughterCondition> prioAdultFemale = new List<SlaughterCondition>();
        public List<SlaughterCondition> prioYoungFemale = new List<SlaughterCondition>();
        public SlaughterPreference defaultAgePref = SlaughterPreference.OldestFirst;

        public List<SlaughterCondition> PrioOf(bool male, bool adult) =>
            male ? (adult ? prioAdultMale : prioYoungMale) : (adult ? prioAdultFemale : prioYoungFemale);

        public bool AnyPrioConditions() =>
            (prioAdultMale != null && prioAdultMale.Count > 0) ||
            (prioYoungMale != null && prioYoungMale.Count > 0) ||
            (prioAdultFemale != null && prioAdultFemale.Count > 0) ||
            (prioYoungFemale != null && prioYoungFemale.Count > 0);

        /// <summary>True when this kind deviates from vanilla behaviour and must be recomputed.</summary>
        public bool Customized =>
            malePref == SlaughterPreference.YoungestFirst ||
            femalePref == SlaughterPreference.YoungestFirst ||
            maleYoungPref == SlaughterPreference.YoungestFirst ||
            femaleYoungPref == SlaughterPreference.YoungestFirst ||
            defaultAgePref == SlaughterPreference.YoungestFirst ||
            AnyPrioConditions() ||
            (keepTraits != null && keepTraits.Count > 0) ||
            (cullTraits != null && cullTraits.Count > 0) ||
            (spareTraits != null && spareTraits.Count > 0) ||
            (forceCullTraits != null && forceCullTraits.Count > 0);

        /// <summary>Priority customization: sex×age (older/younger) prefs, condition lists, or cull/spare trait lists.</summary>
        public bool HasPrioritySettings =>
            malePref == SlaughterPreference.YoungestFirst ||
            femalePref == SlaughterPreference.YoungestFirst ||
            maleYoungPref == SlaughterPreference.YoungestFirst ||
            femaleYoungPref == SlaughterPreference.YoungestFirst ||
            defaultAgePref == SlaughterPreference.YoungestFirst ||
            AnyPrioConditions() ||
            (cullTraits != null && cullTraits.Count > 0) ||
            (spareTraits != null && spareTraits.Count > 0);

        /// <summary>Protection customization: breeding ("keep") trait targets (protect from slaughter).</summary>
        public bool HasProtectionSettings => keepTraits != null && keepTraits.Count > 0;

        /// <summary>Force-slaughter customization: cull matching animals regardless of count/limits.</summary>
        public bool HasForceCullSettings => forceCullTraits != null && forceCullTraits.Count > 0;

        public void Reset()
        {
            malePref = femalePref = maleYoungPref = femaleYoungPref = SlaughterPreference.OldestFirst;
            defaultAgePref = SlaughterPreference.OldestFirst;
            keepTraits.Clear();
            cullTraits.Clear();
            spareTraits.Clear();
            forceCullTraits.Clear();
            prioAdultMale.Clear();
            prioYoungMale.Clear();
            prioAdultFemale.Clear();
            prioYoungFemale.Clear();
        }

        public SlaughterPreference GetPref(bool male, bool adult) =>
            male ? (adult ? malePref : maleYoungPref) : (adult ? femalePref : femaleYoungPref);

        public void SetPref(bool male, bool adult, SlaughterPreference p)
        {
            if (male) { if (adult) malePref = p; else maleYoungPref = p; }
            else { if (adult) femalePref = p; else femaleYoungPref = p; }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref malePref, "malePref", SlaughterPreference.OldestFirst);
            Scribe_Values.Look(ref femalePref, "femalePref", SlaughterPreference.OldestFirst);
            Scribe_Values.Look(ref maleYoungPref, "maleYoungPref", SlaughterPreference.OldestFirst);
            Scribe_Values.Look(ref femaleYoungPref, "femaleYoungPref", SlaughterPreference.OldestFirst);
            Scribe_Collections.Look(ref keepTraits, "keepTraits", LookMode.Deep);
            Scribe_Collections.Look(ref cullTraits, "cullTraits", LookMode.Deep);
            Scribe_Collections.Look(ref spareTraits, "spareTraits", LookMode.Deep);
            Scribe_Collections.Look(ref forceCullTraits, "forceCullTraits", LookMode.Deep);
            Scribe_Values.Look(ref defaultAgePref, "defaultAgePref", SlaughterPreference.OldestFirst);
            Scribe_Collections.Look(ref prioAdultMale, "prioAdultMale", LookMode.Deep);
            Scribe_Collections.Look(ref prioYoungMale, "prioYoungMale", LookMode.Deep);
            Scribe_Collections.Look(ref prioAdultFemale, "prioAdultFemale", LookMode.Deep);
            Scribe_Collections.Look(ref prioYoungFemale, "prioYoungFemale", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (keepTraits == null) keepTraits = new List<TraitTarget>();
                if (cullTraits == null) cullTraits = new List<CullTrait>();
                if (spareTraits == null) spareTraits = new List<CullTrait>();
                if (forceCullTraits == null) forceCullTraits = new List<CullTrait>();
                if (prioAdultMale == null) prioAdultMale = new List<SlaughterCondition>();
                if (prioYoungMale == null) prioYoungMale = new List<SlaughterCondition>();
                if (prioAdultFemale == null) prioAdultFemale = new List<SlaughterCondition>();
                if (prioYoungFemale == null) prioYoungFemale = new List<SlaughterCondition>();
            }
        }
    }
}
