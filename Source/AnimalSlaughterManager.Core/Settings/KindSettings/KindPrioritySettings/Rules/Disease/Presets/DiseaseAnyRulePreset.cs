using System.Xml.Serialization;

namespace ASM;

[XmlRoot("DiseaseAny")]
public class DiseaseAnyRulePreset : PriorityRulePreset
{
    public bool Has { get; set; }
}
