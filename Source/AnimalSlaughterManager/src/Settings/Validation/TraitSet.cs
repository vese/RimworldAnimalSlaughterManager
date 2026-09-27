using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

/// <summary>Base trait set: registers rules with indices; concrete sets take their data in
/// <see cref="AcceptData"/> and run the closure check in <see cref="ValidateClosure"/>.</summary>
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

    /// <summary>Take the concrete data (def names, polarity) from the rules of this set.</summary>
    protected abstract void AcceptData(BasePriorityRule rule);

    public abstract void ValidateClosure(Action<int, string> addError);

    /// <summary>The set's rules in list order.</summary>
    protected IEnumerable<(int index, BasePriorityRule rule)> OrderedRules() =>
        rules.Values.SelectMany(list => list).OrderBy(entry => entry.index);

    /// <summary>Marks the rule redundant, listing the 1-based numbers of the rules that
    /// closed its states.</summary>
    protected static void MarkRedundant(Action<int, string> addError, int index, IEnumerable<int> closers)
    {
        var numbers = closers.Distinct().OrderBy(n => n).Select(n => n + 1);
        addError(index, ASMKeys.ValidationRedundant.Translate(string.Join(", ", numbers)));
    }

    protected static void MarkExhausts(Action<int, string> addError, int index)
    {
        addError(index, ASMKeys.ValidationExhausts.Translate());
    }
}
