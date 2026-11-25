using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Pregnancy")]
public class PregnancyRulePreset : PriorityRulePreset
{
    public bool Has { get; set; }
}
