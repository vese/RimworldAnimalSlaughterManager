using Verse;

namespace ASM;

/// <summary>Age-direction preferences for the four sex×age buckets; descendants provide the save keys.</summary>
public abstract class PreferenceSettings
{
    public SlaughterPreference malePref = SlaughterPreference.OldestFirst;
    public SlaughterPreference maleYoungPref = SlaughterPreference.OldestFirst;
    public SlaughterPreference femalePref = SlaughterPreference.OldestFirst;
    public SlaughterPreference femaleYoungPref = SlaughterPreference.OldestFirst;

    protected abstract string MalePrefKey { get; }
    protected abstract string FemalePrefKey { get; }
    protected abstract string MaleYoungPrefKey { get; }
    protected abstract string FemaleYoungPrefKey { get; }

    public virtual bool Customized =>
        malePref is SlaughterPreference.YoungestFirst ||
        femalePref is SlaughterPreference.YoungestFirst ||
        maleYoungPref is SlaughterPreference.YoungestFirst ||
        femaleYoungPref is SlaughterPreference.YoungestFirst;

    public virtual void Reset()
    {
        malePref = femalePref = maleYoungPref = femaleYoungPref = SlaughterPreference.OldestFirst;
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

    public virtual void ExposeData()
    {
        Scribe_Values.Look(ref malePref, MalePrefKey, SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref femalePref, FemalePrefKey, SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref maleYoungPref, MaleYoungPrefKey, SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref femaleYoungPref, FemaleYoungPrefKey, SlaughterPreference.OldestFirst);
    }
}
