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

    public IEnumerable<ITraitSet> Sets => sets.Values;

    public IEnumerable<IRuleSetValidator> Validators => validators.Values;

    /// <summary>Stores the rule: creates-or-reuses its set, lets the set accept the rule, and
    /// registers the set's validators.</summary>
    public void Add(BasePriorityRule rule, int index)
    {
        if (rule.TraitSetType == null)
        {
            return;
        }

        if (!sets.TryGetValue(rule.TraitSetType, out var set))
        {
            set = (ITraitSet)Activator.CreateInstance(rule.TraitSetType)!;
            sets[rule.TraitSetType] = set;

            foreach (var validator in set.Validators)
            {
                validators.TryAdd(validator.GetType(), validator);
            }
        }

        set.Accept(rule, index);
    }
}
