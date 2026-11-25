using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASM;

/// <summary>
/// The version-1 preset (the format of the released mod, main): the unified
/// "SlaughterPreset" carrier. Read-only — the current version writes its own models; PresetSerializer
/// migrates this into them.
/// </summary>
[XmlRoot("SlaughterPreset")]
public class PresetDto
{
    [XmlElement("Kind")]
    public List<KindPresetDto> Kinds = new List<KindPresetDto>();
}
