using System;
using System.Collections.Generic;
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

    public override IEnumerable<Type> TraitSetTypes
    {
        get
        {
            yield return typeof(BondTraitSet);
        }
    }

    public override IEnumerable<IRuleSetValidator> Validators => [new DuplicateValidator(), new SetClosureValidator()];

    public override bool IsDuplicate(BasePriorityRule other) => other is BondPriorityRule rule && has == rule.has;

    protected override void ChangeVariantInternal() => has = !has;
}
