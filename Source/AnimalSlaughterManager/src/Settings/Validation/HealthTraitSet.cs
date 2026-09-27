using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>Health axis: subsets of the accumulated diseases (sick = non-empty subset).</summary>
public sealed class HealthTraitSet : TraitSet<HealthTraitSet>
{
    public readonly HashSet<string> Diseases = new();

    protected override void AcceptData(BasePriorityRule rule)
    {
        if (rule is DiseasePriorityRule { disease: not null } d)
        {
            Diseases.Add(d.disease.defName);
        }
    }

    public override IReadOnlyList<object> EnumerateStates() => Subsets(Diseases.ToList());

    public override bool Matches(BasePriorityRule rule, object state)
    {
        var diseases = (HashSet<string>)state;

        return rule switch
        {
            DiseaseAnyPriorityRule any => (diseases.Count > 0) == any.has,
            DiseasePriorityRule specific => diseases.Contains(specific.disease?.defName ?? string.Empty) == specific.has,
            _ => true,
        };
    }

    internal static List<HashSet<string>> Subsets(List<string> items)
    {
        var subsets = new List<HashSet<string>>();

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

            subsets.Add(set);
        }

        return subsets;
    }
}
