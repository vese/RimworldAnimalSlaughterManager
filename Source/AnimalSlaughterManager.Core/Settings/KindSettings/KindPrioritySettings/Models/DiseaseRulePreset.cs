using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Disease")]
public class DiseaseRulePreset
{
    public bool has { get; set; }
    public string? disease { get; set; }
}
