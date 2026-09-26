using Verse;

using System;
using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>Single bool axis (pregnant / bonded).</summary>
public sealed class PregnancyTraitSet : TraitSet<PregnancyTraitSet>
{
    public override IEnumerable<object> EnumerateStates()
    {
        yield return false;
        yield return true;
    }

    public override bool Matches(BasePriorityRule rule, object state) =>
        rule is PregnancyPriorityRule typed ? typed.has == (bool)state : true;
}

public sealed class BondTraitSet : TraitSet<BondTraitSet>
{
    public override IEnumerable<object> EnumerateStates()
    {
        yield return false;
        yield return true;
    }

    public override bool Matches(BasePriorityRule rule, object state) =>
        rule is BondPriorityRule typed ? typed.has == (bool)state : true;
}

/// <summary>Health axis: subsets of the accumulated diseases (sick = non-empty subset).</summary>
public sealed class HealthTraitSet : TraitSet<HealthTraitSet>
{
    public readonly HashSet<string> Diseases = new();

    public override IEnumerable<object> EnumerateStates() => Subsets(Diseases.ToList());

    public override bool Matches(BasePriorityRule rule, object state)
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

/// <summary>Trait axis: presence of accumulated traits + polarity; foreignTrait covers animals
/// carrying a trait none of the rules references.</summary>
public sealed class TraitTraitSet : TraitSet<TraitTraitSet>
{
    public readonly HashSet<HediffDef> Traits = new();

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

/// <summary>
/// Exact reachability: the cartesian product of the states of every accumulated set describes
/// every animal the list can distinguish; a rule is unreachable when no combination first-matches
/// it (the rules above always win).
/// </summary>
public sealed class ReachabilityValidator : IRuleSetValidator
{
    public void Validate(RuleValidationContext context, int ruleCount, List<List<string>?> errors)
    {
        var rules = CollectRules(context, ruleCount);
        var setArr = context.Sets.ToArray();
        var statesArr = setArr.Select(s => s.EnumerateStates().ToArray()).ToArray();
        var reachable = new bool[ruleCount];
        var state = new object?[setArr.Length];
        VisitProfiles(setArr, statesArr, state, 0, reachable, rules);

        for (int i = 0; i < ruleCount; i++)
        {
            if (!reachable[i])
            {
                errors[i] ??= [];
                errors[i]!.Add(ASMKeys.ValidationUnreachable.Translate(JoinUpper(i)));
            }
        }
    }

    private static BasePriorityRule[] CollectRules(RuleValidationContext context, int ruleCount)
    {
        var byIndex = new BasePriorityRule[ruleCount];

        foreach (var set in context.Sets)
        {
            foreach (var list in set.Rules.Values)
            {
                foreach (var (index, rule) in list)
                {
                    byIndex[index] = rule;
                }
            }
        }

        return byIndex;
    }

    private static void VisitProfiles(ITraitSet[] sets, object?[][] states, object?[] state, int depth,
        bool[] reachable, BasePriorityRule[] rules)
    {
        if (depth == sets.Length)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                var matched = true;

                for (int s = 0; s < sets.Length; s++)
                {
                    if (!sets[s].Matches(rules[i], state[s]!))
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                {
                    reachable[i] = true;
                    break;
                }
            }

            return;
        }

        foreach (var value in states[depth])
        {
            state[depth] = value;
            VisitProfiles(sets, states, state, depth + 1, reachable, rules);
        }
    }

    private static string JoinUpper(int index) =>
        string.Join(", ", Enumerable.Range(1, index).Select(n => n.ToString()));
}

/// <summary>Duplicates: mutually covering rule pairs (identical targets), reported pairwise
/// with the partner index. Covers cross-type relations too.</summary>
public sealed class DuplicateValidator : IRuleSetValidator
{
    public void Validate(RuleValidationContext context, int ruleCount, List<List<string>?> errors)
    {
        var rules = new BasePriorityRule[ruleCount];

        foreach (var set in context.Sets)
        {
            foreach (var list in set.Rules.Values)
            {
                foreach (var (index, rule) in list)
                {
                    rules[index] = rule;
                }
            }
        }

        for (int i = 0; i < ruleCount - 1; i++)
        {
            for (int j = i + 1; j < ruleCount; j++)
            {
                if (rules[i].Covers(rules[j]) && rules[j].Covers(rules[i]))
                {
                    errors[i] ??= [];
                    errors[j] ??= [];
                    errors[i]!.Add(ASMKeys.ValidationDuplicate.Translate(j + 1));
                    errors[j]!.Add(ASMKeys.ValidationDuplicate.Translate(i + 1));
                }
            }
        }
    }
}
