using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Disease")]
public class DiseaseRulePreset : BasePriorityRulePreset
{
    public bool Has { get; set; }
    public string? Disease { get; set; }

    public override BasePriorityRule ToSetting() => new DiseasePriorityRule(this);
}
