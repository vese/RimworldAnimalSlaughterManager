namespace ASM;

public class KindPreferenceSettings : BasePreferenceSettings
{
    public KindPreferenceSettings() { }

    public KindPreferenceSettings(BasePreferenceSettings source) : base(source) { }

    protected override string MalePrefKey => "malePref";
    protected override string FemalePrefKey => "femalePref";
    protected override string MaleYoungPrefKey => "maleYoungPref";
    protected override string FemaleYoungPrefKey => "femaleYoungPref";
}
