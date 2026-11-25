using System.Xml.Serialization;

namespace ASM;

[XmlRoot("DiseaseAny")]
public class DiseaseAnyRulePreset
{
    public bool Has { get; set; }
}
