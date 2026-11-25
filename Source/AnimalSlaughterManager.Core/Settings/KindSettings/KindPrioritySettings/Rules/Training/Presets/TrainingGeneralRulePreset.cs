using System.Xml.Serialization;

namespace ASM;

[XmlRoot("TrainingGeneral")]
public class TrainingGeneralRulePreset : PriorityRulePreset
{
    public string Type { get; set; } = null!;
}
