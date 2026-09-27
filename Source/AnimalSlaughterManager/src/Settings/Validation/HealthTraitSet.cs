using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>Health axis: per-disease «sick with it / not sick with it» flags plus the aggregate
/// «healthy» and «sick with any of them» axes. Disease combinations are not enumerated — a
/// specific-disease rule only depends on its own def, so coverage factorizes per axis.</summary>
public sealed class HealthTraitSet : TraitSet<HealthTraitSet>
{
    private readonly HashSet<string> diseases = new();

    protected override void AcceptData(BasePriorityRule rule)
    {
        if (rule is DiseasePriorityRule { disease: not null } d)
        {
            diseases.Add(d.disease.defName);
        }
    }

    public override void ValidateClosure(List<List<string>?> errors)
    {
        var has = new Dictionary<string, int>();     // defName → index of the rule that closed «sick with it»
        var notHas = new Dictionary<string, int>(); // … «not sick with it»
        int? healthyClaimer = null;                  // «sick with none of the mentioned diseases»
        int? anyClaimer = null;                      // «sick with at least one of them»

        bool HealthyCovered() => healthyClaimer.HasValue || notHas.Count > 0;

        bool SickCovered() => anyClaimer.HasValue
            || has.Keys.Any(notHas.ContainsKey)
            || diseases.All(d => has.ContainsKey(d));

        bool SpaceCovered() => HealthyCovered() && SickCovered();

        IEnumerable<int> AllClaimers() =>
            has.Values
                .Concat(notHas.Values)
                .Concat(healthyClaimer.HasValue ? new[] { healthyClaimer.Value } : [])
                .Concat(anyClaimer.HasValue ? new[] { anyClaimer.Value } : []);

        foreach (var (index, rule) in OrderedRules())
        {
            switch (rule)
            {
                case DiseasePriorityRule { disease: not null } specific:
                {
                    var def = specific.disease.defName;

                    if (specific.has)
                    {
                        if (has.TryGetValue(def, out var flagCloser))
                        {
                            MarkRedundant(errors, index, [flagCloser]);
                            continue;
                        }

                        if (anyClaimer is int anyIndex)
                        {
                            MarkRedundant(errors, index, [anyIndex]);
                            continue;
                        }

                        if (SpaceCovered())
                        {
                            MarkRedundant(errors, index, AllClaimers());
                            continue;
                        }

                        has[def] = index;
                    }
                    else
                    {
                        if (notHas.TryGetValue(def, out var flagCloser))
                        {
                            MarkRedundant(errors, index, [flagCloser]);
                            continue;
                        }

                        if (diseases.Count == 1 && healthyClaimer is int healthyIndex)
                        {
                            // The only mentioned disease: «healthy» closes the same single state.
                            MarkRedundant(errors, index, [healthyIndex]);
                            continue;
                        }

                        if (SpaceCovered())
                        {
                            MarkRedundant(errors, index, AllClaimers());
                            continue;
                        }

                        notHas[def] = index;
                    }

                    if (SpaceCovered())
                    {
                        MarkExhausts(errors, index);
                    }

                    break;
                }

                case DiseaseAnyPriorityRule any:
                {
                    if (any.has)
                    {
                        if (anyClaimer is int claimer)
                        {
                            MarkRedundant(errors, index, [claimer]);
                            continue;
                        }

                        if (SickCovered())
                        {
                            MarkRedundant(errors, index, AllClaimers());
                            continue;
                        }

                        anyClaimer = index;
                    }
                    else
                    {
                        if (healthyClaimer is int claimer)
                        {
                            MarkRedundant(errors, index, [claimer]);
                            continue;
                        }

                        if (HealthyCovered())
                        {
                            MarkRedundant(errors, index, notHas.Values);
                            continue;
                        }

                        healthyClaimer = index;
                    }

                    if (SpaceCovered())
                    {
                        MarkExhausts(errors, index);
                    }

                    break;
                }
            }
        }
    }
}
