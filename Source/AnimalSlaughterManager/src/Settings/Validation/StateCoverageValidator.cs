using Verse;
using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>
/// Combined coverage over the accumulated trait sets: the cartesian product of every set's
/// states describes every animal the list can distinguish. A rule that no combination
/// first-matches is redundant — by the time it is reached, every animal it could pick has been
/// taken by a combination of the rules above (which pairwise coverage may miss, e.g. a full
/// training-status triple above absorbing everything below).
/// </summary>
public sealed class StateCoverageValidator : IRuleSetValidator
{
    public void Validate(RuleValidationContext context, IReadOnlyList<BasePriorityRule> rules, List<List<string>?> errors)
    {
        var setArr = context.Sets.ToArray();
        var statesArr = setArr.Select(s => s.EnumerateStates().ToArray()).ToArray();
        var matched = new bool[rules.Count];
        var state = new object?[setArr.Length];
        VisitProfiles(setArr, statesArr, state, 0, matched, rules);

        for (int i = 0; i < rules.Count; i++)
        {
            if (!matched[i])
            {
                errors[i] ??= [];
                errors[i]!.Add(ASMKeys.ValidationRedundant.Translate(JoinUpper(i)));
            }
        }
    }

    private static void VisitProfiles(ITraitSet[] sets, object?[][] states, object?[] state, int depth,
        bool[] matched, IReadOnlyList<BasePriorityRule> rules)
    {
        if (depth == sets.Length)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                var isMatch = true;

                for (int s = 0; s < sets.Length; s++)
                {
                    if (!sets[s].Matches(rules[i], state[s]!))
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    matched[i] = true;
                    break;
                }
            }

            return;
        }

        foreach (var value in states[depth])
        {
            state[depth] = value;
            VisitProfiles(sets, states, state, depth + 1, matched, rules);
        }
    }

    private static string JoinUpper(int index) =>
        string.Join(", ", Enumerable.Range(1, index).Select(n => n.ToString()));
}
