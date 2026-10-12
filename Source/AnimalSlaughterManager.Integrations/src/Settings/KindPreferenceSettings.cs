namespace ASM;

public class KindPreferenceSettings : PreferenceSettings
{
    public KindPreferenceSettings() { }

    public KindPreferenceSettings(PreferenceSettings source) : base(source) { }

    protected override string MalePrefKey => "malePref";
    protected override string FemalePrefKey => "femalePref";
    protected override string MaleYoungPrefKey => "maleYoungPref";
    protected override string FemaleYoungPrefKey => "femaleYoungPref";
}
