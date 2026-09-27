using Verse;
using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>
/// The cartesian product of the states of every accumulated set describes every animal the list
/// can distinguish. A rule that no combination first-matches is redundant: by the time it is
/// reached, every animal it could pick by its trait has already been taken by the rules above.
/// </summary>
public sealed class RedundantRuleValidator : IRuleSetValidator
{
    public void Validate(RuleValidationContext context, int ruleCount, List<List<string>?> errors)
    {
        var rules = CollectRules(context, ruleCount);
        var setArr = context.Sets.ToArray();
        var statesArr = setArr.Select(s => s.EnumerateStates().ToArray()).ToArray();
        var meaningful = new bool[ruleCount];
        var state = new object?[setArr.Length];
        VisitProfiles(setArr, statesArr, state, 0, meaningful, rules);

        for (int i = 0; i < ruleCount; i++)
        {
            if (!meaningful[i])
            {
                errors[i] ??= [];
                errors[i]!.Add(ASMKeys.ValidationRedundant.Translate(JoinUpper(i)));
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
        bool[] meaningful, BasePriorityRule[] rules)
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
                    meaningful[i] = true;
                    break;
                }
            }

            return;
        }

        foreach (var value in states[depth])
        {
            state[depth] = value;
            VisitProfiles(sets, states, state, depth + 1, meaningful, rules);
        }
    }

    private static string JoinUpper(int index) =>
        string.Join(", ", Enumerable.Range(1, index).Select(n => n.ToString()));
}
