namespace ASM;

/// <summary>The factory of a rule with a parameterless constructor for its preset model:
/// creates the rule and fills it from the preset.</summary>
public sealed class PriorityRuleFactory<TRule, TPreset> : IPriorityRuleFactory<TPreset>
    where TRule : BasePriorityRule, new()
    where TPreset : BasePriorityRulePreset
{
    public BasePriorityRule Create(BasePriorityRulePreset preset)
    {
        var rule = new TRule();
        rule.FromPreset(preset);

        return rule;
    }
}
