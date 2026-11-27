namespace ASM;

/// <summary>The factory of a rule with a parameterless constructor for its preset model.</summary>
public sealed class PriorityRuleFactory<TRule, TPreset> : IPriorityRuleFactory<TPreset>
    where TRule : BasePriorityRule, new()
    where TPreset : BasePriorityRulePreset
{
    public BasePriorityRule Create() => new TRule();
}
