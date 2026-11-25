using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Training")]
public class TrainingRulePreset
{
    public bool has { get; set; }
    public string? trainable { get; set; }
}
