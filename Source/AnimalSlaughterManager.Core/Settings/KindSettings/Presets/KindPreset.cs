using System.Collections.Generic;

namespace ASM;

/// <summary>The per-kind slice of a preset: prefs, typed rule buckets and the integration
/// settings as their own objects (each integration owns its section format). Built and read
/// by KindSettings.ToPreset/FromPreset; the file form is Presets' business.</summary>
public class KindPreset
{
    /// <summary>The kind's defName.</summary>
    public string Animal { get; set; } = "";

    public SlaughterPreference MalePref { get; set; }
    public SlaughterPreference FemalePref { get; set; }
    public SlaughterPreference MaleYoungPref { get; set; }
    public SlaughterPreference FemaleYoungPref { get; set; }

    public List<BasePriorityRulePreset> PrioRulesAdultMale { get; set; } = [];
    public List<BasePriorityRulePreset> PrioRulesYoungMale { get; set; } = [];
    public List<BasePriorityRulePreset> PrioRulesAdultFemale { get; set; } = [];
    public List<BasePriorityRulePreset> PrioRulesYoungFemale { get; set; } = [];

    /// <summary>The integration settings of the slice by their provider key; the file layer
    /// (de)serializes their sections.</summary>
    public Dictionary<string, IKindSettings> Integrations { get; set; } = [];

    public List<BasePriorityRulePreset> GetBucket((bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => PrioRulesAdultMale,
        (true, false) => PrioRulesYoungMale,
        (false, true) => PrioRulesAdultFemale,
        _ => PrioRulesYoungFemale,
    };

    public void SetBucket((bool Male, bool Adult) bucket, List<BasePriorityRulePreset> presets)
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
