using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The file form of the full preset: every configured kind's file slice. The model
/// mapping is Presets' business (see <see cref="AllKindsPreset"/>).</summary>
[XmlRoot("AllKindsPreset")]
public class AllKindsPresetFile
{
    [XmlElement("Kind")]
    public List<KindPresetFile> Kinds = [];

    /// <summary>The file form of the model.</summary>
    public static AllKindsPresetFile Of(AllKindsPreset preset) => new() { Kinds = [.. preset.Kinds.Select(k => new KindPresetFile { Kind = k })] };

    /// <summary>The model of the file form.</summary>
    public AllKindsPreset ToPreset() => new() { Kinds = [.. Kinds.Select(k => k.Kind)] };
}
