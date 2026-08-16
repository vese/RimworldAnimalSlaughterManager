using RimWorld;
using Verse;

namespace ASM;

public class TraitPriorityRule : BasePriorityRule
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

    public override bool IsInvalid(BasePriorityRule baseRule) => baseRule is TraitPriorityRule rule &&
        // TODO: for TraitInheritability.Both and other any
        trait?.defName == rule.trait?.defName && inheritability == rule.inheritability;

    public override void ChangeVariant() => has = !has;

    private static string DefName(Def? d) => d == null ? Constants.MissingLabel : d.LabelCap.ToString();

    private static bool InheritMatch(HediffDef trait, TraitInheritability mode) => mode switch
    {
        TraitInheritability.Inheritable => AnimalTraitsAccess.IsInheritable(trait),
        TraitInheritability.NonInheritable => !AnimalTraitsAccess.IsInheritable(trait),
        _ => true,
    };
}
