using System.Collections.Generic;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

[XmlRoot("SlaughterPreset")]
public class PresetDto
{
    /// <summary>The preset format version (<c>PresetFormat.CurrentVersion</c>); 0 in files
    /// saved before versioning. Stamped by PresetSerializer on save.</summary>
    [XmlAttribute("version")]
    public int Version;

    [XmlElement("Kind")]
    public List<KindPresetDto> Kinds = new List<KindPresetDto>();

}
