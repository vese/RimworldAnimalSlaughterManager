using System;
using Verse;

namespace ASM;

/// <summary>Age-direction preferences for the four sex×age buckets; descendants provide the save keys.</summary>
public abstract class BasePreferenceSettings : IPresettable
{
    public BasePreferenceSettings() { }

    public BasePreferenceSettings(BasePreferenceSettings source)
    {
        malePref = source.malePref;
        femalePref = source.femalePref;
        maleYoungPref = source.maleYoungPref;
        femaleYoungPref = source.femaleYoungPref;
    }

    public SlaughterPreference malePref = SlaughterPreference.OldestFirst;
    public SlaughterPreference maleYoungPref = SlaughterPreference.OldestFirst;
    public SlaughterPreference femalePref = SlaughterPreference.OldestFirst;
    public SlaughterPreference femaleYoungPref = SlaughterPreference.OldestFirst;

    protected abstract string MalePrefKey { get; }
    protected abstract string FemalePrefKey { get; }
    protected abstract string MaleYoungPrefKey { get; }
    protected abstract string FemaleYoungPrefKey { get; }

    public bool Customized =>
        malePref is SlaughterPreference.YoungestFirst ||
        femalePref is SlaughterPreference.YoungestFirst ||
        maleYoungPref is SlaughterPreference.YoungestFirst ||
        femaleYoungPref is SlaughterPreference.YoungestFirst;

    public void Reset()
    {
        malePref = femalePref = maleYoungPref = femaleYoungPref = SlaughterPreference.OldestFirst;
    }

    public void Reset(BasePreferenceSettings source)
    {
        malePref = source.malePref;
        femalePref = source.femalePref;
        maleYoungPref = source.maleYoungPref;
        femaleYoungPref = source.femaleYoungPref;
    }

    public bool Matches(BasePreferenceSettings other) =>
        malePref == other.malePref &&
        femalePref == other.femalePref &&
        maleYoungPref == other.maleYoungPref &&
        femaleYoungPref == other.femaleYoungPref;

    public SlaughterPreference Get(bool male, bool adult) =>
        male ? (adult ? malePref : maleYoungPref) : (adult ? femalePref : femaleYoungPref);

    public void Set(bool male, bool adult, SlaughterPreference p)
    {
        if (Get(male, adult) == p)
        {
            return;
        }

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

        SettingsChangeTracker.Raise();
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref malePref, MalePrefKey, SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref femalePref, FemalePrefKey, SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref maleYoungPref, MaleYoungPrefKey, SlaughterPreference.OldestFirst);
        Scribe_Values.Look(ref femaleYoungPref, FemaleYoungPrefKey, SlaughterPreference.OldestFirst);
    }

    public void Save(KindPreset dto)
    {
        dto.MalePref = malePref;
        dto.FemalePref = femalePref;
        dto.MaleYoungPref = maleYoungPref;
        dto.FemaleYoungPref = femaleYoungPref;
    }

    public void Load(KindPreset dto)
    {
        malePref = dto.MalePref;
        femalePref = dto.FemalePref;
        maleYoungPref = dto.MaleYoungPref;
        femaleYoungPref = dto.FemaleYoungPref;
    }
}
