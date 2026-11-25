using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Training")]
public class TrainingRulePreset
{
    public bool has = true;
    public string? trainable;
}
