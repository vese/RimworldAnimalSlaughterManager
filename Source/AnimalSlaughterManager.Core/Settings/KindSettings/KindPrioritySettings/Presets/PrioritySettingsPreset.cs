using System.Collections.Generic;

namespace ASM;

/// <summary>The preset model of the kind's priority settings: the four age×sex rule buckets
/// (top = keep, bottom = cull).</summary>
public class PrioritySettingsPreset
{
    public List<BasePriorityRulePreset> PriorityRulesAdultMale { get; set; } = [];
    public List<BasePriorityRulePreset> PriorityRulesYoungMale { get; set; } = [];
    public List<BasePriorityRulePreset> PriorityRulesAdultFemale { get; set; } = [];
    public List<BasePriorityRulePreset> PriorityRulesYoungFemale { get; set; } = [];

    public List<BasePriorityRulePreset> GetBucket((bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => PriorityRulesAdultMale,
        (true, false) => PriorityRulesYoungMale,
        (false, true) => PriorityRulesAdultFemale,
        (false, false) => PriorityRulesYoungFemale,
    };

    public void SetBucket((bool Male, bool Adult) bucket, List<BasePriorityRulePreset> presets)
    {
        switch (bucket)
        {
            case (true, true):
                PriorityRulesAdultMale = presets;
                break;
            case (true, false):
                PriorityRulesYoungMale = presets;
                break;
            case (false, true):
                PriorityRulesAdultFemale = presets;
                break;
            case (false, false):
                PriorityRulesYoungFemale = presets;
                break;
        }
    }
}
