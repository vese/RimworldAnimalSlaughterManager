using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-2 file model of a priority-list preset: the rule bucket's Rule
/// elements.</summary>
[XmlRoot("PriorityList")]
public class V2PriorityListPreset : V2RulePreset
{
    private const string RulesElement = "Rules";

    [XmlAnyElement]
    public XmlElement[]? Rules;

    public static V2PriorityListPreset OfRules(System.Collections.Generic.List<BasePriorityRulePreset> rules) => new() { Rules = [RulePresetXml.Bucket(RulesElement, PriorityRuleXml.ToRulePresets(rules))] };

    public System.Collections.Generic.List<BasePriorityRulePreset> ToRuleList() => PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket((Rules ?? [])[0]));
}
