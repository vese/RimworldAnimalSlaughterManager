using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-2 file model of a special-list preset: the owning settings' own
/// section elements.</summary>
[XmlRoot("SpecialList")]
public class V2SpecialListPreset
{
    [XmlAnyElement]
    public XmlElement[]? Sections;
}
