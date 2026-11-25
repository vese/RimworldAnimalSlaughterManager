using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Bond")]
public class BondRulePreset : PriorityRulePreset
{
    public bool Has { get; set; }
}
