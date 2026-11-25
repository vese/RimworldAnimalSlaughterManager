using System.Xml.Serialization;

namespace ASM;

[XmlRoot("TrainingGeneral")]
public class TrainingGeneralRulePreset
{
    public string Type { get; set; } = null!;
}
