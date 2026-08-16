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

    public override BasePriorityRule Clone() => new TraitGeneralPriorityRule() { type = type, inheritability = inheritability };

    public override bool Matches(Pawn? p) => type switch
    {
        TraitType.Both => inheritability switch
        {
            TraitInheritability.Both => HasTrait(p) == has,
            TraitInheritability.Inheritable => HasTrait(p) == has,
            TraitInheritability.NonInheritable => HasTrait(p) == has,
            _ => throw new NotImplementedException(),
        },
        TraitType.Positive => inheritability switch
        {
            TraitInheritability.Both => HasTrait(p, false) == has,
            TraitInheritability.Inheritable => HasTrait(p, false) == has,
            TraitInheritability.NonInheritable => HasTrait(p, false) == has,
            _ => throw new NotImplementedException(),
        },
        TraitType.Negative => inheritability switch
        {
            TraitInheritability.Both => HasTrait(p, true) == has,
            TraitInheritability.Inheritable => HasTrait(p, true) == has,
            TraitInheritability.NonInheritable => HasTrait(p, true) == has,
            _ => throw new NotImplementedException(),
        },
        _ => throw new NotImplementedException(),
    };

    public override void ExposeData()
    {
        Scribe_Values.Look(ref type, "type", TraitType.Both);
        Scribe_Values.Look(ref inheritability, "inheritability", TraitInheritability.Both);
    }

    public override bool IsInvalid(BasePriorityRule baseRule) => baseRule is TraitGeneralPriorityRule rule &&
        // TODO: for Both
        type == rule.type && inheritability == rule.inheritability;

    public override void ChangeVariant()
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
