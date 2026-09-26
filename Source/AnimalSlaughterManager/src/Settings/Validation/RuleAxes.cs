using System.Collections.Generic;

namespace ASM;

/// <summary>
/// One animal property axis a family of rules matches on (pregnancy, bond, health, training,
/// traits). An axis enumerates the animal states distinguishable by the rules assigned to it —
/// accumulating the concrete settings (def names, flags) it met — and tells whether a rule
/// matches a state. Rules of a foreign axis match every state: a rule is constrained by its own
/// axis only.
/// </summary>
public interface IRuleAxis
{
    /// <summary>Animal states this axis distinguishes; <paramref name="rules"/> are the rules
    /// assigned to this axis (the validator groups them), carrying the settings to accumulate.</summary>
    IEnumerable<object> EnumerateStates(IReadOnlyList<BasePriorityRule> rules);

    /// <summary>Whether the rule matches the axis state. Foreign-axis rules match everything.</summary>
    bool Matches(BasePriorityRule rule, object state);
}

/// <summary>Rules with a single bool animal state (pregnant / bonded).</summary>
public abstract class BoolAxis<TRule> : IRuleAxis where TRule : BasePriorityRule
{
    protected abstract bool Has(TRule rule);

    public IEnumerable<object> EnumerateStates(IReadOnlyList<BasePriorityRule> rules)
    {
        yield return false;
        yield return true;
    }

    public bool Matches(BasePriorityRule rule, object state) =>
        rule is TRule typed ? Has(typed) == (bool)state : true;
}

public class PregnancyAxis : BoolAxis<PregnancyPriorityRule>
{
    public static readonly PregnancyAxis Instance = new();

    private PregnancyAxis() { }

    protected override bool Has(PregnancyPriorityRule rule) => rule.has;
}

public class BondAxis : BoolAxis<BondPriorityRule>
{
    public static readonly BondAxis Instance = new();

    private BondAxis() { }

    protected override bool Has(BondPriorityRule rule) => rule.has;
}
