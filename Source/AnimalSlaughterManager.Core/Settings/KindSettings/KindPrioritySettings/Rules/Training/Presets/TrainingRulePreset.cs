using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Training")]
public class TrainingRulePreset
{
    public bool Has { get; set; }
    public string? Trainable { get; set; }
}
