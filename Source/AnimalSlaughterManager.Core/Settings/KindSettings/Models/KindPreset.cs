using System.Collections.Generic;

namespace ASM;

/// <summary>The per-kind slice of a preset: prefs, typed rule buckets and the integration
/// settings as their own objects (each integration owns its section format). Built and read
/// by KindSettings.ToPreset/FromPreset; the file form is Presets' business.</summary>
public class KindPreset
{
    public string animal = "";
    public string malePref = "";
    public string femalePref = "";
    public string maleYoungPref = "";
    public string femaleYoungPref = "";

    public List<PriorityRulePreset> PrioRulesAdultMale = [];
    public List<PriorityRulePreset> PrioRulesYoungMale = [];
    public List<PriorityRulePreset> PrioRulesAdultFemale = [];
    public List<PriorityRulePreset> PrioRulesYoungFemale = [];

    /// <summary>The integration settings of the slice by their provider key; the file layer
    /// (de)serializes their sections.</summary>
    public Dictionary<string, IKindSettings> Integrations = [];

    public List<PriorityRulePreset> GetBucket((bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => PrioRulesAdultMale,
        (true, false) => PrioRulesYoungMale,
        (false, true) => PrioRulesAdultFemale,
        _ => PrioRulesYoungFemale,
    };

    public void SetBucket((bool Male, bool Adult) bucket, List<PriorityRulePreset> presets)
    {
        switch (bucket)
        {
            case (true, true):
                PrioRulesAdultMale = presets;
                break;
            case (true, false):
                PrioRulesYoungMale = presets;
                break;
            case (false, true):
                PrioRulesAdultFemale = presets;
                break;
            default:
                PrioRulesYoungFemale = presets;
                break;
        }
    }
}
