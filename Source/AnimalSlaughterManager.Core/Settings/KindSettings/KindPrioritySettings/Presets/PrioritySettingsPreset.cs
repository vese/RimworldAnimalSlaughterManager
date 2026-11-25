using System.Collections.Generic;

namespace ASM;

/// <summary>The preset model of the kind's priority settings: the four age×sex rule buckets
/// (top = keep, bottom = cull).</summary>
public class PrioritySettingsPreset
{
    public List<BasePriorityRulePreset> PrioRulesAdultMale { get; set; } = [];
    public List<BasePriorityRulePreset> PrioRulesYoungMale { get; set; } = [];
    public List<BasePriorityRulePreset> PrioRulesAdultFemale { get; set; } = [];
    public List<BasePriorityRulePreset> PrioRulesYoungFemale { get; set; } = [];

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
