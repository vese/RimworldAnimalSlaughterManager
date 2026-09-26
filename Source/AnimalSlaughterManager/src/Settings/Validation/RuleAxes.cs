using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>
/// One animal property axis a family of rules matches on (pregnancy, bond, health, training,
/// traits). An axis enumerates the animal states distinguishable by the rules referencing it —
/// accumulating the concrete settings (def names, flags) it met — and tells whether a rule
/// matches a state. Rules of a foreign axis match every state: a rule is constrained by its own
/// axis only.
/// Add-on rules plug into validation by registering an axis for their rule type via
/// <see cref="RuleAxes.Register"/>.
/// </summary>
public interface IRuleAxis
{
    /// <summary>Animal states this axis distinguishes for the given rules.</summary>
    IEnumerable<object> EnumerateStates(IReadOnlyList<BasePriorityRule> rules);

    /// <summary>Whether the rule matches the axis state. Foreign-axis rules match everything.</summary>
    bool Matches(BasePriorityRule rule, object state);
}

public static class RuleAxes
{
    private static readonly Dictionary<Type, IRuleAxis> axes = new()
    {
        [typeof(PregnancyPriorityRule)] = new PregnancyAxis(),
        [typeof(BondPriorityRule)] = new BondAxis(),
        [typeof(DiseaseAnyPriorityRule)] = new HealthAxis(),
        [typeof(DiseasePriorityRule)] = new HealthAxis(),
        [typeof(TrainingGeneralPriorityRule)] = new TrainingAxis(),
        [typeof(TrainingPriorityRule)] = new TrainingAxis(),
        [typeof(TraitGeneralPriorityRule)] = new TraitAxis(),
        [typeof(TraitPriorityRule)] = new TraitAxis(),
    };

    public static void Register<TRule>(IRuleAxis axis) where TRule : BasePriorityRule => axes[typeof(TRule)] = axis;

    public static IRuleAxis? For(BasePriorityRule rule) => axes.TryGetValue(rule.GetType(), out var axis) ? axis : null;
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
    protected override bool Has(PregnancyPriorityRule rule) => rule.has;
}

public class BondAxis : BoolAxis<BondPriorityRule>
{
    protected override bool Has(BondPriorityRule rule) => rule.has;
}
