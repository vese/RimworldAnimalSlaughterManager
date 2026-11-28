using System.Xml;

namespace ASM;

/// <summary>The integration's own per-kind special settings: the key and the factory of the
/// runtime settings (everything about them lives on ISpecialSettings itself). The version-1
/// reading seam — the preset model from its XML section among a kind element's children —
/// stays in the contract because the Legacy folder resolves every provider for it.</summary>
public interface IKindSettingsProvider
{
    string Key { get; }

    ISpecialSettings Create();

    /// <summary>The preset model read from its own section (or the legacy flat children) among
    /// the kind element's children; null when a section of this integration was not
    /// there.</summary>
    BaseSpecialSettingsPreset? ReadSection(XmlElement kind);
}
