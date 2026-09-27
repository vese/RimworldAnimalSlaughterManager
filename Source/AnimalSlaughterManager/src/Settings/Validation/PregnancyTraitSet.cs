using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>Pregnancy axis: the «pregnant»/«not pregnant» states, closed by rule flags.</summary>
public sealed class PregnancyTraitSet : TraitSet<PregnancyTraitSet>
{
    protected override void AcceptData(BasePriorityRule rule) { }

    public override void ValidateClosure(Action<int, string> addError)
    {
        var closed = new Dictionary<bool, int>();

        foreach (var (index, rule) in OrderedRules())
        {
            var has = ((PregnancyPriorityRule)rule).has;

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
