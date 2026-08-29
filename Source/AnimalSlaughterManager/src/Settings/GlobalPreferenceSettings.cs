using System;

namespace ASM;

public class GlobalPreferenceSettings : PreferenceSettings
{
    /// <summary>Raised after a global pref is set; subscribers apply it to per-kind settings.</summary>
    public event Action<bool, bool, SlaughterPreference>? PrefChanged;

    public override void Set(bool male, bool adult, SlaughterPreference p)
    {
        base.Set(male, adult, p);
        PrefChanged?.Invoke(male, adult, p);
    }

    protected override string MalePrefKey => "globalMalePref";
    protected override string FemalePrefKey => "globalFemalePref";
    protected override string MaleYoungPrefKey => "globalMaleYoungPref";
    protected override string FemaleYoungPrefKey => "globalFemaleYoungPref";
}
