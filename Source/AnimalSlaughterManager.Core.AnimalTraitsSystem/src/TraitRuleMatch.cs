using RimWorld;
using Verse;

namespace ASM;

/// <summary>Age/gender/inheritability filters shared by the ATS keep and force-cull rules.</summary>
internal static class TraitRuleMatch
{
    public static bool AgeMatches(Pawn p, AgeScope scope)
    {
        bool repro = p.ageTracker.CurLifeStage.reproductive;

        switch (scope)
        {
            case AgeScope.Adult:
                return repro;
            case AgeScope.Young:
                return !repro;
            default:
                return true;
        }
    }

    public static bool GenderMatches(Pawn p, GenderScope scope)
    {
        switch (scope)
        {
            case GenderScope.Male:
                return p.gender == Gender.Male;
            case GenderScope.Female:
                return p.gender == Gender.Female;
            default:
                return true;
        }
    }

    public static bool InheritableMatches(HediffDef trait, TraitInheritability mode)
    {
        switch (mode)
        {
            case TraitInheritability.Inheritable:
                return AnimalTraitsAccess.IsInheritable(trait);
            case TraitInheritability.NonInheritable:
                return !AnimalTraitsAccess.IsInheritable(trait);
            default:
                return true;
        }
    }

    public static bool Matches(Pawn pawn, TraitProtectRule rule) =>
        rule.trait != null        && AnimalTraitsAccess.HasTrait(pawn, rule.trait)        && AgeMatches(pawn, rule.ageScope)        && GenderMatches(pawn, rule.genderScope)        && InheritableMatches(rule.trait, rule.inheritMode);

    public static bool Matches(Pawn pawn, TraitRule rule) =>
        rule.trait != null        && AnimalTraitsAccess.HasTrait(pawn, rule.trait)        && AgeMatches(pawn, rule.ageScope)        && GenderMatches(pawn, rule.genderScope)        && InheritableMatches(rule.trait, rule.inheritMode);
}
