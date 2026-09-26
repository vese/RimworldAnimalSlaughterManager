using RimWorld;
using Verse;

namespace ASM;

public class BondPriorityRule : BasePriorityRule
{
    public bool has = true;

    public override string Label => has ? ASMKeys.CondBondHas.Translate() : ASMKeys.CondBondMissing.Translate();

    public override BasePriorityRule Clone() => new BondPriorityRule() { has = has };

    public override bool MatchesSignals(in AnimalSignals signals) => signals.bonded == has;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref has, "has", true);
    }

    public override bool Covers(BasePriorityRule other) => other is BondPriorityRule rule && has == rule.has;

    protected override void ChangeVariantInternal() => has = !has;
}
