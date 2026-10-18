using RimWorld;
using System.Collections.Generic;
using System;
using Verse;
using Verse.Sound;

namespace ASM;

public class TraitGeneralPriorityRule : BasePriorityRule, ICoversRule<TraitGeneralPriorityRule>, ICoversRule<TraitPriorityRule>
{
    public bool has = true;
    public TraitType type = TraitType.Both;
    public TraitInheritability inheritability = TraitInheritability.Both;

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

    private static readonly Type[] traitSetTypes = [typeof(AnimalTraitSet)];

    public override IEnumerable<Type> TraitSetTypes => traitSetTypes;

    public override bool IsDuplicate(BasePriorityRule other) =>
        other is TraitGeneralPriorityRule rule && type == rule.type && inheritability == rule.inheritability;

    public bool Covers(TraitGeneralPriorityRule other)
    {
        if (inheritability != other.inheritability)
        {
            return false;
        }

        // "Any trait" covers "positive/negative only" for "has".
        if (type == other.type || (has && type == TraitType.Both && other.type != TraitType.Both))
        {
            return true;
        }

        // "Without positive/negative" covers "without any trait".
        return !has && type != TraitType.Both && other.type == TraitType.Both;
    }

    // A specific positive/negative trait ⊆ the matching "has" general rule.
    public bool Covers(TraitPriorityRule other) =>
        other.has == has &&
        inheritability == TraitInheritability.Both &&
        type != TraitType.Both &&
        other.trait != null &&
        other.trait.isBad == (type == TraitType.Negative);

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
