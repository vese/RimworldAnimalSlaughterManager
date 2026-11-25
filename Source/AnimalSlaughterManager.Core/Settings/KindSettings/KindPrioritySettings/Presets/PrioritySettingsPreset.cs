using System.Collections.Generic;

namespace ASM;

/// <summary>One priority bucket as a list preset: the typed rule presets.</summary>
public class PrioritySettingsPreset
{
    public List<BasePriorityRulePreset> Rules { get; set; }
}
