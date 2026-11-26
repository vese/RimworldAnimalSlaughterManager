using System.Xml;
using System.Xml.Serialization;

namespace ASM;

// TODO: переименовать в соответствии с другими моделями
/// <summary>The file form of one rule preset: the rule class name and the rule's own DTO
/// serialized as the payload. The DTO types are plain (no shared base): XmlSerializer takes
/// the concrete type, the name routes the payload on read.</summary>
[XmlRoot("Rule")]
public class RulePreset
{
    [XmlAttribute]
    public string Type = "";

    /// <summary>The serialized rule DTO (its element is the DTO's own root).</summary>
    [XmlAnyElement]
    public XmlElement[]? Payload;
}
