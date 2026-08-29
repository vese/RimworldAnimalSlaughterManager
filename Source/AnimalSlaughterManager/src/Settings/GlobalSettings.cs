using Verse;

namespace ASM;

/// <summary>Map-wide slaughter settings (General tab). ExposeData() delegates, so the XML keys stay flat in the parent node.</summary>
public class GlobalSettings : IExposable
{
    /// <summary>Global default age-direction prefs; per-kind prefs override these.</summary>
    public GlobalPreferenceSettings preferenceSettings = new();

    public GlobalSettings() { }

    public GlobalSettings(SettingsChanges changes)
    {
        preferenceSettings.Changes = changes;
    }

    public void Reset() => preferenceSettings.Reset();

    public void ExposeData()
    {
        preferenceSettings.ExposeData();
    }
}
