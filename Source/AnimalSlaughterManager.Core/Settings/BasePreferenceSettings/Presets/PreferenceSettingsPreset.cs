namespace ASM;

/// <summary>The preset model of the age×sex slaughter preferences: the cull direction of each
/// of the four groups.</summary>
public class PreferenceSettingsPreset
{
    public SlaughterPreference MalePref { get; set; }
    public SlaughterPreference FemalePref { get; set; }
    public SlaughterPreference MaleYoungPref { get; set; }
    public SlaughterPreference FemaleYoungPref { get; set; }
}
