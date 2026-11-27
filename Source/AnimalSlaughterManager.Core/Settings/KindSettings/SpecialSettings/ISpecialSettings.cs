using Verse;

namespace ASM;

/// <summary>
/// The per-kind special settings of an integration (e.g. the ATS keep/force-cull trait
/// lists): a preset model like the preference and priority settings — it builds and reads
/// its own model — plus the save scribing (IExposable, the save's integrationData node) and
/// the customized query. The XML section of the preset model is the provider's business.
/// </summary>
public interface ISpecialSettings : IPresettable<BaseSpecialSettingsPreset>, IExposable
{
    /// <summary>True when the settings deviate from the default and the kind must be treated
    /// as customized.</summary>
    bool Customized { get; }

    void Reset();
}
