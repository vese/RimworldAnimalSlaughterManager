using System.Collections.Generic;

namespace ASM;

public sealed class PregnancyTraitSet : TraitSet<PregnancyTraitSet>
{
    protected override void AcceptData(BasePriorityRule rule) { }

    public override IEnumerable<object> EnumerateStates()
    {
        yield return false;
        yield return true;
    }

    public override bool Matches(BasePriorityRule rule, object state) =>
        rule is PregnancyPriorityRule typed ? typed.has == (bool)state : true;
}
