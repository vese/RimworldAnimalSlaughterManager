using System;

namespace ASM;

/// <summary>Pregnancy axis: the «pregnant»/«not pregnant» states, closed by rule flags.</summary>
public sealed class PregnancyTraitSet : TraitSet<PregnancyTraitSet>
{
    protected override void AcceptData(BasePriorityRule rule) { }

    public override void ValidateClosure(Action<int, string> addProblem)
    {
        int? closedPregnant = null;
        int? closedNotPregnant = null;

        foreach (var (index, rule) in OrderedRules())
        {
            var has = ((PregnancyPriorityRule)rule).has;
            var alreadyClosed = has ? closedPregnant : closedNotPregnant;

            if (alreadyClosed is int closer)
            {
                MarkRedundant(addProblem, index, [closer]);
                continue;
            }

            if (has)
            {
                closedPregnant = index;
            }
            else
            {
                closedNotPregnant = index;
            }

            if (closedPregnant.HasValue && closedNotPregnant.HasValue)
            {
                MarkExhausts(addProblem, index);
            }
        }
    }
}
