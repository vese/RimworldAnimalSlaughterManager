using System;

namespace ASM;

/// <summary>A rule factory over the rule's own DTO-taking constructor.</summary>
public sealed class PriorityRuleFactory<TPreset>(Func<TPreset, BasePriorityRule> create) : IPriorityRuleFactory<TPreset>
    where TPreset : BasePriorityRulePreset
{
    public BasePriorityRule Create(BasePriorityRulePreset preset) => create((TPreset)preset);
}
