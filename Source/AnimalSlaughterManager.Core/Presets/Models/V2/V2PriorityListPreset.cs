using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-2 file model of a priority-list preset: the ordered rules (the rule
/// DTOs themselves, (de)serialized polymorphically — the content serializer knows every
/// registered rule type).</summary>
[XmlRoot("PriorityList")]
public class V2PriorityListPreset
{
    public List<BasePriorityRulePreset> Rules = [];
}
