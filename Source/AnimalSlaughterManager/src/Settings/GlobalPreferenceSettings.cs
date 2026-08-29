namespace ASM;

/// <summary>Map-wide preference settings (General tab). Save keys: "globalMalePref", …</summary>
public class GlobalPreferenceSettings : PreferenceSettings
{
    protected override string MalePrefKey => "globalMalePref";
    protected override string FemalePrefKey => "globalFemalePref";
    protected override string MaleYoungPrefKey => "globalMaleYoungPref";
    protected override string FemaleYoungPrefKey => "globalFemaleYoungPref";
}
