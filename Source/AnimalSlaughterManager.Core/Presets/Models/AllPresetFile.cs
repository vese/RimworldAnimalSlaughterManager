using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The file form of the full preset: every configured kind's file slice. The model
/// mapping is Presets' business (see <see cref="AllPreset"/>).</summary>
[XmlRoot("AllPreset")]
public class AllPresetFile
{
    [XmlElement("Kind")]
    public List<KindPresetFile> Kinds = [];

    /// <summary>The file form of the model.</summary>
    public static AllPresetFile Of(AllPreset preset) => new() { Kinds = [.. preset.Kinds.Select(k => new KindPresetFile { Kind = k })] };

    /// <summary>The model of the file form.</summary>
    public AllPreset ToPreset() => new() { Kinds = [.. Kinds.Select(k => k.Kind)] };
}
