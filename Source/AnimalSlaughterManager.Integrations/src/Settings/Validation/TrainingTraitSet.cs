using System;
using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>Training axis: per-skill «learned / not learned» flags plus the shared training
/// status axis (none/partial/full). Skill combinations are not enumerated — a specific-skill
/// rule only depends on its own def.</summary>
public sealed class TrainingTraitSet : TraitSet<TrainingTraitSet>
{
    public enum Status
    {
        None,
        Partial,
        Full,
    }

    private static readonly Status[] AllStatuses = [Status.None, Status.Partial, Status.Full];

    private readonly HashSet<string> skills = new();

    protected override void AcceptData(BasePriorityRule rule)
    {
        if (rule is TrainingPriorityRule { trainable: not null } t)
        {
            skills.Add(t.trainable.defName);
        }
    }

    public override void ValidateClosure(Action<int, string> addProblem)
    {
        var has = new Dictionary<string, int>();        // defName → index of the rule that closed «learned»
        var notHas = new Dictionary<string, int>();    // … «not learned»
        var statuses = new Dictionary<Status, int>();  // status → index of the rule that closed it

        bool SkillsCovered() => notHas.Count > 0
            && (has.Keys.Any(notHas.ContainsKey) || skills.All(s => has.ContainsKey(s)));

        bool SpaceCovered() => AllStatuses.All(s => statuses.ContainsKey(s)) || SkillsCovered();

        IEnumerable<int> SkillClaimers() => has.Values.Concat(notHas.Values);

        foreach (var (index, rule) in OrderedRules())
        {
            switch (rule)
            {
                case TrainingPriorityRule { trainable: not null } specific:
                {
                    var def = specific.trainable.defName;
                    var flags = specific.has ? has : notHas;

                    if (flags.TryGetValue(def, out var flagCloser))
                    {
                        MarkRedundant(addProblem, index, [flagCloser]);
                        continue;
                    }

                    if (AllStatuses.All(s => statuses.ContainsKey(s)))
                    {
                        MarkRedundant(addProblem, index, statuses.Values);
                        continue;
                    }

                    if (SkillsCovered())
                    {
                        MarkRedundant(addProblem, index, SkillClaimers());
                        continue;
                    }

                    flags[def] = index;

                    if (SpaceCovered())
                    {
                        MarkExhausts(addProblem, index);
                    }

                    break;
                }

                case TrainingGeneralPriorityRule general:
                {
                    var matched = StatusesOf(general.type).ToList();

                    if (matched.All(s => statuses.ContainsKey(s)))
                    {
                        MarkRedundant(addProblem, index, matched.Select(s => statuses[s]));
                        continue;
                    }

                    if (SkillsCovered())
                    {
                        MarkRedundant(addProblem, index, SkillClaimers());
                        continue;
                    }

                    foreach (var status in matched)
                    {
                        statuses.TryAdd(status, index);
                    }

                    if (SpaceCovered())
                    {
                        MarkExhausts(addProblem, index);
                    }

                    break;
                }
            }
        }
    }

    private static IEnumerable<Status> StatusesOf(TrainingGeneralType type)
    {
        switch (type)
        {
            case TrainingGeneralType.None:
                return [Status.None];
            case TrainingGeneralType.Partial:
                return [Status.Partial];
            case TrainingGeneralType.PartialOrFull:
                return [Status.Partial, Status.Full];
            case TrainingGeneralType.Full:
                return [Status.Full];
            default:
                return [];
        }
    }
}
