using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

/// <summary>Trait axis: presence of accumulated traits + polarity; foreignTrait covers animals
/// carrying a trait none of the rules references.</summary>
public sealed class TraitTraitSet : TraitSet<TraitTraitSet>
{
    public readonly HashSet<HediffDef> Traits = new();

    protected override void AcceptData(BasePriorityRule rule)
    {
        if (rule is TraitPriorityRule { trait: not null } t)
        {
            Traits.Add(t.trait);
        }
    }

    public override IEnumerable<object> EnumerateStates()
    {
        var traits = Traits.ToList();

        foreach (var subset in HealthTraitSet.Subsets(traits.Select(t => t.defName).ToList()))
        {
            bool positive = traits.Any(t => subset.Contains(t.defName) && !t.isBad);
            bool negative = traits.Any(t => subset.Contains(t.defName) && t.isBad);

            yield return (subset, positive, negative, false);
            yield return (subset, true, false, true);
            yield return (subset, false, true, true);
        }
    }

    public override bool Matches(BasePriorityRule rule, object state)
    {
        var (present, positive, negative, foreignTrait) = ((HashSet<string>, bool, bool, bool))state;

        return rule switch
        {
            TraitGeneralPriorityRule general => general.type switch
            {
                TraitType.Both => (positive || negative || foreignTrait) == general.has,
                TraitType.Positive => positive == general.has,
                TraitType.Negative => negative == general.has,
                _ => true,
            },
            TraitPriorityRule specific => present.Contains(specific.trait?.defName ?? "") == specific.has,
            _ => true,
        };
    }
}
