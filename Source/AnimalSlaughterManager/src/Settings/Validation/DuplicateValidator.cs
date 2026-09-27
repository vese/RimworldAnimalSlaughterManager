using Verse;
using System.Collections.Generic;

namespace ASM;

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
