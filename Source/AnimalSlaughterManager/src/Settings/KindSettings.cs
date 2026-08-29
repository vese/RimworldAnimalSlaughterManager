using Verse;

namespace ASM;

/// <summary>
/// Per-animal-kind slaughter settings, layered on top of vanilla AutoSlaughterConfig.
/// Slaughter priority is split into four sex×age buckets (matching vanilla's buckets).
/// </summary>
public class KindSettings : IExposable
{
    public KindPreferenceSettings preferenceSettings;
    public KindPrioritySettings prioritySettings = new();
    public KindTraitsSettings traitsSettings = new();

    public KindSettings()
    {
        preferenceSettings = new();
    }

    public KindSettings(GlobalSettings globals)
    {
        preferenceSettings = new(globals.preferenceSettings);
    }


    /// <summary>True when this kind deviates from vanilla behaviour and must be recomputed.</summary>
    public bool Customized => preferenceSettings.Customized || prioritySettings.HasRules || traitsSettings.Customized;

    /// <summary>Protection customization: breeding ("keep") trait targets (protect from slaughter).</summary>
    public bool HasProtectionSettings => traitsSettings.HasProtectionSettings;

    /// <summary>Force-slaughter customization: cull matching animals regardless of count/limits.</summary>
    public bool HasForceCullSettings => traitsSettings.HasForceCullSettings;

    public void Reset()
    {
        preferenceSettings.Reset();
        prioritySettings.Reset();
        traitsSettings.Reset();
    }

    public void Reset(GlobalSettings globals)
    {
        preferenceSettings.Reset(globals.preferenceSettings);
        prioritySettings.Reset();
        traitsSettings.Reset();
    }

    public void ExposeData()
    {
        preferenceSettings.ExposeData();
        prioritySettings.ExposeData();
        traitsSettings.ExposeData();
    }
}
