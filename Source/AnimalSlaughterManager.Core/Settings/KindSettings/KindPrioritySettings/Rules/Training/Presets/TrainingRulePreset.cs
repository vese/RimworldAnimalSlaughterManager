using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Training")]
public class TrainingRulePreset : PriorityRulePreset
{
    public bool Has { get; set; }
    public string? Trainable { get; set; }
}
