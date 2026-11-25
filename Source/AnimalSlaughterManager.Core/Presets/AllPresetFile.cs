using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The file form of the full preset: the version plus every configured kind's file
/// slice. Built and applied through ASM_MapComp.ToPreset/FromPreset.</summary>
[XmlRoot("AllPreset")]
public class AllPresetFile : IVersionedPresetDto
{
    [XmlAttribute("version")]
    public int Version { get; set; }

    [XmlElement("Kind")]
    public List<KindPresetFile> Kinds = [];
}
