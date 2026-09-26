using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>
/// Health axis: sick with any of the referenced diseases, or healthy. The state is the set of
/// diseases an animal carries (from those the rules mention); DiseaseAny checks non-emptiness.
/// </summary>
public class HealthAxis : IRuleAxis
{
    public IEnumerable<object> EnumerateStates(IReadOnlyList<BasePriorityRule> rules)
    {
        var diseases = rules.OfType<DiseasePriorityRule>()
            .Select(d => d.disease?.defName)
            .Where(n => n != null)
            .Distinct()
            .ToList();

        foreach (var subset in Subsets(diseases!))
        {
            yield return subset;
        }
    }

    public bool Matches(BasePriorityRule rule, object state)
    {
        var diseases = (HashSet<string>)state;

        return rule switch
        {
            DiseaseAnyPriorityRule any => (diseases.Count > 0) == any.has,
            DiseasePriorityRule specific => diseases.Contains(specific.disease?.defName ?? "") == specific.has,
            _ => true,
        };
    }

    internal static IEnumerable<HashSet<string>> Subsets(List<string> items)
    {
        for (long mask = 0; mask < 1L << items.Count; mask++)
        {
            var set = new HashSet<string>();

            for (int b = 0; b < items.Count; b++)
            {
                if ((mask & 1L << b) != 0)
                {
                    set.Add(items[b]);
                }
            }

            yield return set;
        }
    }
}

/// <summary>
/// Training axis: none / partial / full, crossed with the learned flags of the referenced
/// trainable skills.
/// </summary>
public class TrainingAxis : IRuleAxis
{
    public enum Status
    {
        None,
        Partial,
        Full,
    }

    public IEnumerable<object> EnumerateStates(IReadOnlyList<BasePriorityRule> rules)
    {
        var skills = rules.OfType<TrainingPriorityRule>()
            .Select(t => t.trainable?.defName)
            .Where(n => n != null)
            .Distinct()
            .ToList();

        foreach (var status in new[] { Status.None, Status.Partial, Status.Full })
        {
            foreach (var subset in HealthAxis.Subsets(skills!))
            {
                yield return (status, subset);
            }
        }
    }

    public bool Matches(BasePriorityRule rule, object state)
    {
        var (status, skills) = ((Status, HashSet<string>))state;

        return rule switch
        {
            TrainingGeneralPriorityRule general => general.type switch
            {
                TrainingGeneralType.None => status == Status.None,
                TrainingGeneralType.Partial => status == Status.Partial,
                TrainingGeneralType.PartialOrFull => status != Status.None,
                TrainingGeneralType.Full => status == Status.Full,
                _ => true,
            },
            TrainingPriorityRule specific => skills.Contains(specific.trainable?.defName ?? "") == specific.has,
            _ => true,
        };
    }
}

/// <summary>
/// Trait axis: trait polarity flags crossed with the presence of each referenced trait. Trait
/// polarity is derived from the trait defs the rules reference (vanilla HediffDef.isBad).
/// </summary>
public class TraitAxis : IRuleAxis
{
    public IEnumerable<object> EnumerateStates(IReadOnlyList<BasePriorityRule> rules)
    {
        var traits = rules.OfType<TraitPriorityRule>()
            .Select(t => t.trait)
            .Where(t => t != null)
            .Distinct()
            .ToList();

        foreach (var subset in HealthAxis.Subsets(traits!.Select(t => t.defName).ToList()))
        {
            bool positive = traits!.Any(t => subset.Contains(t.defName) && !t.isBad);
            bool negative = traits!.Any(t => subset.Contains(t.defName) && t.isBad);

            // Also the states where a foreign trait (not referenced by the rules) is present.
            yield return (subset, positive, negative, false);
            yield return (subset, true, false, true);
            yield return (subset, false, true, true);
        }
    }

    public bool Matches(BasePriorityRule rule, object state)
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
