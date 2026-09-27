using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ASM;

public class TraitPriorityRule : BasePriorityRule, ICoversRule<TraitPriorityRule>
{
    public bool has = true;
    public HediffDef? trait;
    public TraitInheritability inheritability = TraitInheritability.Both;

    public override bool HasNullDef => trait == null;

    public override string Label => (has ? ASMKeys.CondHas : ASMKeys.CondMissing).Translate(DefName(trait));

    public override BasePriorityRule Clone() => new TraitPriorityRule() { has = has, trait = trait, inheritability = inheritability };

    public override bool Matches(Pawn? p) => trait != null && AnimalTraitsAccess.HasTrait(p, trait) == has && InheritMatch(trait, inheritability);

    public override void ExposeData()
    {
        Scribe_Values.Look(ref has, "has", true);
        Scribe_Defs.Look(ref trait, "trait");
        Scribe_Values.Look(ref inheritability, "inheritability", TraitInheritability.Both);
    }

    public override IEnumerable<Type> TraitSetTypes
    {
        get
        {
            yield return typeof(AnimalTraitSet);
        }
    }

    public override IEnumerable<IRuleSetValidator> Validators => [new DuplicateValidator(), new CoverageValidator<TraitPriorityRule, TraitPriorityRule>(), new SetClosureValidator()];

    public override bool IsDuplicate(BasePriorityRule other) =>
        other is TraitPriorityRule rule &&
        trait?.defName == rule.trait?.defName &&
        has == rule.has &&
        inheritability == rule.inheritability;

        /// "Any inheritability" matches every animal the narrower filter matches.
        public bool Covers(TraitPriorityRule other) =>
            trait?.defName == other.trait?.defName &&
            has == other.has &&
            (inheritability == TraitInheritability.Both || inheritability == other.inheritability);

    protected override void ChangeVariantInternal() => has = !has;

    private static string DefName(Def? d) => d == null ? Constants.MissingLabel : d.LabelCap.ToString();

    private static bool InheritMatch(HediffDef trait, TraitInheritability mode) => mode switch
    {
        TraitInheritability.Inheritable => AnimalTraitsAccess.IsInheritable(trait),
        TraitInheritability.NonInheritable => !AnimalTraitsAccess.IsInheritable(trait),
        _ => true,
    };
}
