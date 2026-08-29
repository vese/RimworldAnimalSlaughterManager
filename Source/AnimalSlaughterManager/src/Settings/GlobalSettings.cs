using Verse;

namespace ASM;

/// <summary>
/// Map-wide (global) slaughter settings — the General tab. Structured like <see cref="KindSettings"/>:
/// holds sub-settings objects, and <see cref="ExposeData"/> delegates to them, so the XML keys stay
/// flat in the parent node.
/// </summary>
public class GlobalSettings : IExposable
{
    /// <summary>
    /// Global default age-direction prefs. Per-kind prefs (KindSettings.preferenceSettings)
    /// override these on the Priorities tab.
    /// </summary>
    public KindPreferenceSettings preferenceSettings = new();

    public void Reset() => preferenceSettings.Reset();

    public void ExposeData()
    {
        // Flat save keys ("globalMalePref", …) — part of the on-disk save format.
        preferenceSettings.ExposeGlobalData();
    }
}
