using System.Xml;

namespace ASM;

/// <summary>The integration's own per-kind special settings: the key, the factory and the
/// preset model's XML section (the section's shape is the integration's file-format
/// knowledge). Everything about the runtime settings lives on ISpecialSettings itself.</summary>
public interface IKindSettingsProvider
{
    string Key { get; }

    ISpecialSettings Create();

    /// <summary>Appends the preset section element (named by <see cref="Key"/>) with the
    /// model's contents under <paramref name="parent"/>.</summary>
    void WriteSection(BaseSpecialSettingsPreset preset, XmlElement parent);

    /// <summary>The preset model read from its own section (or the legacy flat children) among
    /// the kind element's children; null when a section of this integration was not there.</summary>
    BaseSpecialSettingsPreset? ReadSection(XmlElement kind);
}
