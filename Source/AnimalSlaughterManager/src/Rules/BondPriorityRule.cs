using RimWorld;
using Verse;

namespace ASM;

public class BondPriorityRule : BasePriorityRule
{
    public bool has = true;

    public override string Label => has ? ASMKeys.CondBondHas.Translate() : ASMKeys.CondBondMissing.Translate();

    public override BasePriorityRule Clone() => new BondPriorityRule() { has = has };

    public override bool Matches(Pawn? p) => (p?.relations?.GetDirectRelationsCount(PawnRelationDefOf.Bond) ?? 0) > 0 == has;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref has, "has", true);
    }

    public override IRuleAxis? Axis => BondAxis.Instance;

    public override bool Covers(BasePriorityRule other) => other is BondPriorityRule rule && has == rule.has;

    protected override void ChangeVariantInternal() => has = !has;
}
