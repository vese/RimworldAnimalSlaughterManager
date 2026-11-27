using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-2 file model of one kind's slice: the def name, the preferences, the
/// rule buckets (polymorphic V2RulePreset items — the serializer knows every registered rule
/// type) and the integration settings (V2SpecialSettingsPreset items under their provider
/// keys).</summary>
public class V2KindPreset
{
    [XmlAttribute]
    public string AnimalDefName = null!;

    public V2PreferenceSettings Preferences = new();

    public List<V2RulePreset> PriorityRulesAdultMale = [];
    public List<V2RulePreset> PriorityRulesYoungMale = [];
    public List<V2RulePreset> PriorityRulesAdultFemale = [];
    public List<V2RulePreset> PriorityRulesYoungFemale = [];

    public List<V2SpecialSettingsPreset> SpecialSettings = [];
}
