using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>Bond axis: the «bonded»/«not bonded» states, closed by rule flags.</summary>
public sealed class BondTraitSet : TraitSet<BondTraitSet>
{
    protected override void AcceptData(BasePriorityRule rule) { }

    public override void ValidateClosure(Action<int, string> addError)
    {
        var closed = new Dictionary<bool, int>();

        foreach (var (index, rule) in OrderedRules())
        {
            var has = ((BondPriorityRule)rule).has;

            if (closed.TryGetValue(has, out var closer))
            {
                MarkRedundant(addError, index, [closer]);
                continue;
            }

            closed[has] = index;

            if (closed.Count == 2)
            {
                MarkExhausts(addError, index);
            }
        }
    }
}
