using System.Collections.Generic;

namespace ASM;

public sealed class BondTraitSet : TraitSet<BondTraitSet>
{
    protected override void AcceptData(BasePriorityRule rule) { }

    public override IEnumerable<object> EnumerateStates()
    {
        yield return false;
        yield return true;
    }

    public override bool Matches(BasePriorityRule rule, object state) =>
        rule is BondPriorityRule typed ? typed.has == (bool)state : true;
}
