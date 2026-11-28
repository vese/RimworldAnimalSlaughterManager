using System.Xml.Serialization;

namespace ASM;

[XmlRoot("Bond")]
public class BondRulePreset : BasePriorityRulePreset
{
    public bool Has { get; set; }

    public override BasePriorityRule ToSetting() => new BondPriorityRule(this);
}
