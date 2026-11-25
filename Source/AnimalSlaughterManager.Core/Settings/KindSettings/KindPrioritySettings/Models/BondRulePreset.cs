using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Bond")]
public class BondRulePreset
{
    public bool has { get; set; }
}
