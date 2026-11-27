using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-2 file model of one kind's slice: the def name, the preferences, the
/// rule buckets and the integration settings — all as the Settings DTOs themselves, (de)serialized
/// polymorphically (the content serializer knows every registered subtype).</summary>
public class V2KindPreset
{
    [XmlAttribute]
    public string AnimalDefName = null!;

    public V2PreferenceSettings Preferences = new();

    public List<BasePriorityRulePreset> PriorityRulesAdultMale = [];
    public List<BasePriorityRulePreset> PriorityRulesYoungMale = [];
    public List<BasePriorityRulePreset> PriorityRulesAdultFemale = [];
    public List<BasePriorityRulePreset> PriorityRulesYoungFemale = [];

    public List<V2SpecialSettingsEntry> SpecialSettings = [];
}
