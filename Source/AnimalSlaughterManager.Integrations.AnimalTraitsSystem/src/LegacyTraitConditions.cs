using System;
namespace ASM;

/// <summary>
/// Legacy trait conditions → the ATS rules: the serialized form stores the ATS enum names
/// (inheritMode, polarity), which only this integration understands.
/// </summary>
public sealed class LegacyTraitConditions : ILegacyConditionConverter
{
    public BasePriorityRule? Convert(SlaughterCondition condition) => condition.type switch
    {
        CondType.Trait => new TraitPriorityRule
        {
            has = condition.has,
            trait = condition.trait,
            inheritability = ParseInheritability(condition.inheritMode),
        },
        CondType.HasPositiveTrait => new TraitGeneralPriorityRule
        {
            has = condition.has,
            type = TraitType.Positive,
        },
        CondType.HasNegativeTrait => new TraitGeneralPriorityRule
        {
            has = condition.has,
            type = TraitType.Negative,
        },
        _ => null,
    };

    private static TraitInheritability ParseInheritability(string value) =>
        Enum.TryParse(value, out TraitInheritability parsed) ? parsed : TraitInheritability.Both;
}
