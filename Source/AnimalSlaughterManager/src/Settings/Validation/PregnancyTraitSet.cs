using System.Collections.Generic;

namespace ASM;

/// <summary>Pregnancy axis: the «pregnant»/«not pregnant» states, closed by rule flags.</summary>
public sealed class PregnancyTraitSet : TraitSet<PregnancyTraitSet>
{
    protected override void AcceptData(BasePriorityRule rule) { }

    public override void ValidateClosure(List<List<string>?> errors)
    {
        var closed = new Dictionary<bool, int>();

        foreach (var (index, rule) in OrderedRules())
        {
            var has = ((PregnancyPriorityRule)rule).has;

            if (closed.TryGetValue(has, out var closer))
            {
                MarkRedundant(errors, index, [closer]);
                continue;
            }

            closed[has] = index;

            if (closed.Count == 2)
            {
                MarkExhausts(errors, index);
            }
        }
    }
}
