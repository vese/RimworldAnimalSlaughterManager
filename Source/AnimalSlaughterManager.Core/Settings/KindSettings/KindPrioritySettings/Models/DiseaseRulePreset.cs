using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Disease")]
public class DiseaseRulePreset
{
    public bool has = true;
    public string? disease;
}
