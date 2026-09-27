using Verse;
using System.Collections.Generic;

namespace ASM;

/// <summary>Coverage across rule types, in list order: when an earlier rule of another type
/// covers a later one (e.g. "sick" over "sick with flu"), the later rule never fires.</summary>
public sealed class CrossTypeCoverageValidator : IRuleSetValidator
{
    public void Validate(RuleValidationContext context, IReadOnlyList<BasePriorityRule> rules, List<List<string>?> errors)
    {
        for (int i = 0; i < rules.Count - 1; i++)
        {
            for (int j = i + 1; j < rules.Count; j++)
            {
                if (rules[i].GetType() == rules[j].GetType())
                {
                    continue;
                }

                if (rules[i].Covers(rules[j]))
                {
                    errors[j] ??= [];
                    errors[j]!.Add(ASMKeys.ValidationRedundant.Translate(i + 1));
                }
            }
        }
    }
}
