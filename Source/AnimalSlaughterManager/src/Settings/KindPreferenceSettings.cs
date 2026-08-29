namespace ASM;

public class KindPreferenceSettings : PreferenceSettings
{
    public KindPreferenceSettings() { }

    public KindPreferenceSettings(SettingsChanges changes, PreferenceSettings source) : base(source)
    {
        Changes = changes;
    }

    protected override string MalePrefKey => "malePref";
    protected override string FemalePrefKey => "femalePref";
    protected override string MaleYoungPrefKey => "maleYoungPref";
    protected override string FemaleYoungPrefKey => "femaleYoungPref";
}
