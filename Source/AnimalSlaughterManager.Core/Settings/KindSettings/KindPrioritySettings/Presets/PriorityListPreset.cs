using System.Collections.Generic;

namespace ASM;

/// <summary>A priority rule list as the Settings-side model: one age×sex bucket's ordered
/// rules.</summary>
public class PriorityListPreset : BasePreset
{
    public List<BasePriorityRulePreset> Rules { get; set; } = [];
}
