using System.Xml;

namespace ASM;

/// <summary>The integration's own special settings: the key, the model factory and every
/// piece of behavior around the model — queries, reset, save scribing and the preset XML
/// section. The core stores the models opaquely and never names their concrete type.</summary>
public interface IKindSettingsProvider
{
    string Key { get; }

    BaseSpecialSettingsPreset Create();

    /// <summary>True when the settings deviate from the default and the kind must be treated
    /// as customized.</summary>
    bool Customized(BaseSpecialSettingsPreset preset);

    void Reset(BaseSpecialSettingsPreset preset);

    /// <summary>Saves/loads the model through Scribe under its node (the provider owns the
    /// concrete type, so the save's LookMode.Deep reaches its IExposable).</summary>
    void ExposeData(BaseSpecialSettingsPreset preset);

    /// <summary>Appends the preset section element (named by <see cref="Key"/>) with the
    /// model's contents under <paramref name="parent"/>.</summary>
    void WriteSection(BaseSpecialSettingsPreset preset, XmlElement parent);

    /// <summary>The model read from its own section (or the legacy flat children) among the
    /// kind element's children; null when a section of this integration was not there.</summary>
    BaseSpecialSettingsPreset? ReadSection(XmlElement kind);
}
