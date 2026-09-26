using RimWorld;
using System;
using Verse;
using Verse.Sound;

namespace ASM;

public class TraitGeneralPriorityRule : BasePriorityRule
{
    public bool has = true;
    public TraitType type = TraitType.Both;
    public TraitInheritability inheritability = TraitInheritability.Both;

    public override bool HasExtraParameters { get; } = true;

    public override string Label => type switch
    {
        TraitType.Both => inheritability switch
        {
            TraitInheritability.Both => throw new NotImplementedException(),
            TraitInheritability.Inheritable => throw new NotImplementedException(),
            TraitInheritability.NonInheritable => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        },
        TraitType.Positive => inheritability switch
        {
            TraitInheritability.Both => ASMKeys.CondPositiveHas.Translate(),
            TraitInheritability.Inheritable => throw new NotImplementedException(),
            TraitInheritability.NonInheritable => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        },
        TraitType.Negative => inheritability switch
        {
            TraitInheritability.Both => ASMKeys.CondNegativeHas.Translate(),
            TraitInheritability.Inheritable => throw new NotImplementedException(),
            TraitInheritability.NonInheritable => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        },
        _ => throw new NotImplementedException(),
    };

    public override BasePriorityRule Clone() => new TraitGeneralPriorityRule() { has = has, type = type, inheritability = inheritability };

    public override bool Matches(Pawn? p) => type switch
    {
        TraitType.Both => HasTrait(p) == has,
        TraitType.Positive => HasTrait(p, false) == has,
        TraitType.Negative => HasTrait(p, true) == has,
        _ => throw new NotImplementedException(),
    };

    public override void ExposeData()
    {
        Scribe_Values.Look(ref type, "type", TraitType.Both);
        Scribe_Values.Look(ref inheritability, "inheritability", TraitInheritability.Both);
    }

    public override bool Covers(BasePriorityRule other)
    {
        if (other is TraitGeneralPriorityRule general)
        {
            if (inheritability != general.inheritability)
            {
                return false;
            }

            // Same target is a duplicate; "any trait" covers "positive/negative only" for "has".
            if (type == general.type || (has && type == TraitType.Both && general.type != TraitType.Both))
            {
                return true;
            }

            // "Without positive/negative" covers "without any trait".
            return !has && type != TraitType.Both && general.type == TraitType.Both;
        }

        // A specific positive/negative trait ⊆ the matching "has" general rule.
        return other is TraitPriorityRule trait &&
               trait.has == has &&
               inheritability == TraitInheritability.Both &&
               type != TraitType.Both &&
               trait.trait != null &&
               trait.trait.isBad == (type == TraitType.Negative);
    }

    protected override void ChangeVariantInternal()
    {
        has = !has;

        if (has)
        {
            type = type switch
            {
                TraitType.Both => TraitType.Positive,
                TraitType.Positive => TraitType.Negative,
                TraitType.Negative => TraitType.Both,
                _ => throw new NotImplementedException(),
            };
        }
    }

    private static bool HasTrait(Pawn? p, bool? isBad = null)
    {
        foreach (var def in AnimalTraitsAccess.KnownTraitDefs)
        {
            if (AnimalTraitsAccess.HasTrait(p, def) && (!isBad.HasValue || def.isBad == isBad.Value))
            {
                return true;
            }
        }

        return false;
    }
}
