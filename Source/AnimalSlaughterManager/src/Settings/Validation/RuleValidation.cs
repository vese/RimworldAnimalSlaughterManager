using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>
/// An accumulated trait set (pregnancy, training skills, traits, …): rules of one animal axis
/// register themselves here with their list index, and the set accumulates the concrete data
/// (def names, flags) needed to enumerate distinguishable animal states and match rules.
/// </summary>
public interface ITraitSet
{
    /// <summary>Rules registered in this set by rule type, with their indices in the list.</summary>
    IReadOnlyDictionary<Type, List<(int index, BasePriorityRule rule)>> Rules { get; }

    /// <summary>Animal states this set distinguishes by its accumulated data.</summary>
    IEnumerable<object> EnumerateStates();

    /// <summary>Whether the rule matches the state; foreign-set rules match everything.</summary>
    bool Matches(BasePriorityRule rule, object state);
}

/// <summary>Base trait set: rule registration with indices; concrete sets add their data.</summary>
public abstract class TraitSet<TSelf> : ITraitSet where TSelf : TraitSet<TSelf>, new()
{
    private readonly Dictionary<Type, List<(int index, BasePriorityRule rule)>> rules = new();

    public IReadOnlyDictionary<Type, List<(int index, BasePriorityRule rule)>> Rules => rules;

    public void AddRule(int index, BasePriorityRule rule)
    {
        if (!rules.TryGetValue(rule.GetType(), out var list))
        {
            list = [];
            rules[rule.GetType()] = list;
        }

        list.Add((index, rule));
    }

    public abstract IEnumerable<object> EnumerateStates();

    public abstract bool Matches(BasePriorityRule rule, object state);
}

/// <summary>Validates accumulated trait sets: marks unreachable rules, duplicates, or any other
/// cross-type incompatibility, writing per-index problem messages into the errors list.</summary>
public interface IRuleSetValidator
{
    void Validate(RuleValidationContext context, int ruleCount, List<List<string>?> errors);
}

/// <summary>
/// Per-validation accumulator. The pass over the rules asks each rule to
/// <see cref="BasePriorityRule.Accumulate"/>: the rule creates-or-updates the trait set of its
/// axis (adding itself with its index) and registers its validators. After the pass, every
/// registered validator runs once over the accumulated data.
/// </summary>
public sealed class RuleValidationContext
{
    private readonly Dictionary<Type, ITraitSet> sets = new();
    private readonly Dictionary<Type, IRuleSetValidator> validators = new();

    public IEnumerable<ITraitSet> Sets => sets.Values;

    public IEnumerable<IRuleSetValidator> Validators => validators.Values;

    /// <summary>The set of type T, created on first request.</summary>
    public TSet GetSet<TSet>() where TSet : ITraitSet, new()
    {
        if (!sets.TryGetValue(typeof(TSet), out var set))
        {
            set = new TSet();
            sets[typeof(TSet)] = set;
        }

        return (TSet)set;
    }

    /// <summary>The validator of type T, registered on first request.</summary>
    public TValidator GetValidator<TValidator>() where TValidator : IRuleSetValidator, new()
    {
        if (!validators.TryGetValue(typeof(TValidator), out var validator))
        {
            validator = new TValidator();
            validators[typeof(TValidator)] = validator;
        }

        return (TValidator)validator;
    }
}
