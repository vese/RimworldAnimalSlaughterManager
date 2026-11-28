using System.Xml.Serialization;

namespace ASM;

[XmlRoot("DiseaseAny")]
public class DiseaseAnyRulePreset : BasePriorityRulePreset
{
    public bool Has { get; set; }

    public override BasePriorityRule ToSetting() => new DiseaseAnyPriorityRule(this);
}
