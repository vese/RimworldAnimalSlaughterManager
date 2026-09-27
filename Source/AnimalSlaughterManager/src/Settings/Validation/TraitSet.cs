using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>Base trait set: registers rules with indices; concrete sets take their data in
/// <see cref="AcceptData"/>. Registers the standard validators (meaningless/duplicate rules).</summary>
public abstract class TraitSet<TSelf> : ITraitSet where TSelf : TraitSet<TSelf>, new()
{
    private readonly Dictionary<Type, List<(int index, BasePriorityRule rule)>> rules = new();

    public IReadOnlyDictionary<Type, List<(int index, BasePriorityRule rule)>> Rules => rules;

    public void Accept(BasePriorityRule rule, int index)
    {
        if (!rules.TryGetValue(rule.GetType(), out var list))
        {
            list = [];
            rules[rule.GetType()] = list;
        }

        list.Add((index, rule));
        AcceptData(rule);
    }

    /// <summary>Take the concrete data (def names, flags) from the rule of this set.</summary>
    protected abstract void AcceptData(BasePriorityRule rule);

    public abstract IReadOnlyList<object> EnumerateStates();

    public abstract bool Matches(BasePriorityRule rule, object state);
}
