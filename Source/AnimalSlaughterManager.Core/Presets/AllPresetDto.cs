using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The full (All-scope) preset: every configured kind's slice. Built and applied by
/// ASM_MapComp.ToPreset/FromPreset.</summary>
[XmlRoot("AllPreset")]
public class AllPresetDto : IVersionedPresetDto
{
    [XmlAttribute]
    public int version { get; set; }

    [XmlElement("Kind")]
    public List<KindPresetDto> Kinds = new List<KindPresetDto>();

    /// <summary>Migration from the version-1 unified format (read by PresetSerializer).</summary>
    public static AllPresetDto FromV1(PresetDto v1) => new()
    {
        Kinds = [.. v1.Kinds]
    };
}
