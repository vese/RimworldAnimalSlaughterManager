using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ASM;

public class DiseaseAnyPriorityRule : BasePriorityRule, ICoversRule<DiseasePriorityRule>
{
    public bool has = true;

    public override string Label => has ? ASMKeys.CondDiseaseAnyHas.Translate() : ASMKeys.CondDiseaseAnyMissing.Translate();

    public override BasePriorityRule Clone() => new DiseaseAnyPriorityRule() { has = has };

    public override bool Matches(Pawn? p) => IsSick(p) == has;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref has, "has", true);
    }

    public override IEnumerable<Type> TraitSetTypes
    {
        get
        {
            yield return typeof(HealthTraitSet);
        }
    }

    public override IEnumerable<IRuleSetValidator> Validators => [new DuplicateValidator(), new CoverageValidator<DiseaseAnyPriorityRule, DiseasePriorityRule>(), new SetClosureValidator()];

    public override bool IsDuplicate(BasePriorityRule other) => other is DiseaseAnyPriorityRule rule && has == rule.has;

        /// Sick with a specific disease ⊆ sick.
        public bool Covers(DiseasePriorityRule other) => has && other.has;

    protected override void ChangeVariantInternal() => has = !has;

    private static bool IsSick(Pawn? p)
    {
        var hediffs = p?.health?.hediffSet?.hediffs;

        if (hediffs == null)
        {
            return false;
        }

        for (int i = 0; i < hediffs.Count; i++)
        {
            var hediff = hediffs[i];

            if (hediff?.def != null && hediff.def.makesSickThought)
            {
                return true;
            }
        }

        return false;
    }
}
