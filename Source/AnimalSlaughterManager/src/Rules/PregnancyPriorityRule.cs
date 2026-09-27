using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ASM;

public class PregnancyPriorityRule : BasePriorityRule
{
    public bool has = true;

    public override string Label => has ? ASMKeys.CondPregnantHas.Translate() : ASMKeys.CondPregnantMissing.Translate();

    public override BasePriorityRule Clone() => new PregnancyPriorityRule() { has = has };

    public override bool Matches(Pawn? p) => ASM_MapComp.IsPregnantOrCarryingEgg(p) == has;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref has, "has", true);
    }

    private static readonly Type[] traitSetTypes = [typeof(PregnancyTraitSet)];

    public override IEnumerable<Type> TraitSetTypes => traitSetTypes;

    public override IEnumerable<IRuleSetValidator> Validators => [new DuplicateValidator(), new SetClosureValidator()];

    public override bool IsDuplicate(BasePriorityRule other) => other is PregnancyPriorityRule rule && has == rule.has;

    protected override void ChangeVariantInternal() => has = !has;
}
