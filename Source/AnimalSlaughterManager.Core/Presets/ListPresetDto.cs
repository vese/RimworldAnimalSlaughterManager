using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>A single rule list preset: the payload is the owning settings' own XML (the
/// priority bucket element or the special settings' section). Built and read by the settings
/// classes — the repository treats it opaquely.</summary>
[XmlRoot("ListPreset")]
public class ListPresetDto : IVersionedPresetDto
{
    [XmlAttribute]
    public int version { get; set; }

    [XmlAnyElement]
    public XmlElement[]? Content;
}
