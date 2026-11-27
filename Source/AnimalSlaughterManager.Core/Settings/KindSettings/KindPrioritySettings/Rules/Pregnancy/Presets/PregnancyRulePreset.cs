using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Pregnancy")]
public class PregnancyRulePreset : BasePriorityRulePreset
{
    public bool Has { get; set; }
}
