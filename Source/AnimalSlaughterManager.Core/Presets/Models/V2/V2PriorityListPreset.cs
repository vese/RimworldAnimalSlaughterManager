using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-2 file model of a priority-list preset: the ordered rules
/// (polymorphic V2RulePreset items — the serializer knows every registered rule type).</summary>
[XmlRoot("PriorityList")]
public class V2PriorityListPreset
{
    public List<V2RulePreset> Rules = [];

    public static V2PriorityListPreset OfRules(List<BasePriorityRulePreset> rules) =>
        new() { Rules = [.. rules.Select(V2RulePresetMapper.ToV2).Where(v2 => v2 != null)] };

    public List<BasePriorityRulePreset> ToRuleList() =>
        [.. Rules.Select(V2RulePresetMapper.ToDto).Where(dto => dto != null)];
}
