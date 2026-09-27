using System.Collections.Generic;

namespace ASM;

public sealed class BondTraitSet : TraitSet<BondTraitSet>
{
    protected override void AcceptData(BasePriorityRule rule) { }

    private static readonly object[] states = [false, true];

    public override IReadOnlyList<object> EnumerateStates() => states;

    public override bool Matches(BasePriorityRule rule, object state) =>
        rule is BondPriorityRule typed ? typed.has == (bool)state : true;
}
