using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Training")]
public class TrainingRulePreset : BasePriorityRulePreset
{
    public bool Has { get; set; }
    public string? Trainable { get; set; }

    public override BasePriorityRule ToSetting() => new TrainingPriorityRule(this);
}
