using System.Xml.Serialization;

namespace ASM;

[XmlRoot("TrainingGeneral")]
public class TrainingGeneralRulePreset : BasePriorityRulePreset
{
    public string Type { get; set; } = null!;

    public override BasePriorityRule ToSetting() => new TrainingGeneralPriorityRule(this);
}
