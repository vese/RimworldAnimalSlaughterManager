using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>Training axis: none/partial/full × learned flags of the accumulated skills.</summary>
public sealed class TrainingTraitSet : TraitSet<TrainingTraitSet>
{
    public enum Status
    {
        None,
        Partial,
        Full,
    }

    public readonly HashSet<string> Skills = new();

    protected override void AcceptData(BasePriorityRule rule)
    {
        if (rule is TrainingPriorityRule { trainable: not null } t)
        {
            Skills.Add(t.trainable.defName);
        }
    }

    public override IEnumerable<object> EnumerateStates()
    {
        foreach (var status in new[] { Status.None, Status.Partial, Status.Full })
        {
            foreach (var subset in HealthTraitSet.Subsets(Skills.ToList()))
            {
                yield return (status, subset);
            }
        }
    }

    public override bool Matches(BasePriorityRule rule, object state)
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
