using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>
/// Per-validation accumulator. The pass feeds rules here: the context asks each rule for its
/// trait set type, creates-or-reuses that set, lets the set take the rule's data, and registers
/// the set's validators. After the pass, every registered validator runs once.
/// </summary>
public sealed class RuleValidationContext
{
    private readonly Dictionary<Type, ITraitSet> sets = new();
    private readonly Dictionary<Type, IRuleSetValidator> validators = new();
    private readonly Dictionary<Type, List<(int index, BasePriorityRule rule)>> rulesByType = new();

    public IEnumerable<ITraitSet> Sets => sets.Values;

    public IEnumerable<IRuleSetValidator> Validators => validators.Values;

    /// <summary>Rules by rule type, with their indices — same-type checks run over these lists.</summary>
    public IReadOnlyDictionary<Type, List<(int index, BasePriorityRule rule)>> RulesByType => rulesByType;

    /// <summary>Stores the rule: creates-or-reuses each of its trait sets, lets every set take
    /// the rule's data, indexes it by rule type, and registers the rule's validators.</summary>
    public void Add(BasePriorityRule rule, int index)
    {
        if (!rulesByType.TryGetValue(rule.GetType(), out var typed))
        {
            typed = [];
            rulesByType[rule.GetType()] = typed;
        }

        typed.Add((index, rule));

        foreach (var setType in rule.TraitSetTypes)
        {
            if (!sets.TryGetValue(setType, out var set))
            {
                set = (ITraitSet)Activator.CreateInstance(setType)!;
                sets[setType] = set;
            }

            set.Accept(rule, index);
        }

        foreach (var validator in rule.Validators)
        {
            validators.TryAdd(validator.GetType(), validator);
        }
    }
}
