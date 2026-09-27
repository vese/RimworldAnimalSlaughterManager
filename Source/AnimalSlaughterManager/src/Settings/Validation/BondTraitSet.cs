using System;

namespace ASM;

/// <summary>Bond axis: the «bonded»/«not bonded» states, closed by rule flags.</summary>
public sealed class BondTraitSet : TraitSet<BondTraitSet>
{
    protected override void AcceptData(BasePriorityRule rule) { }

    public override void ValidateClosure(Action<int, string> addError)
    {
        int? closedBonded = null;
        int? closedNotBonded = null;

        foreach (var (index, rule) in OrderedRules())
        {
            var has = ((BondPriorityRule)rule).has;
            var alreadyClosed = has ? closedBonded : closedNotBonded;

            if (alreadyClosed is int closer)
            {
                MarkRedundant(addError, index, [closer]);
                continue;
            }

            if (has)
            {
                closedBonded = index;
            }
            else
            {
                closedNotBonded = index;
            }

            if (closedBonded.HasValue && closedNotBonded.HasValue)
            {
                MarkExhausts(addError, index);
            }
        }
    }
}
