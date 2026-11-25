using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Disease")]
public class DiseaseRulePreset : PriorityRulePreset
{
    public bool Has { get; set; }
    public string? Disease { get; set; }
}
