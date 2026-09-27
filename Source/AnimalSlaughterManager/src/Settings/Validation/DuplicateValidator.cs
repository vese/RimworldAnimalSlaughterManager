using Verse;
using System.Collections.Generic;

namespace ASM;

/// <summary>Duplicates: identical rules of the same type (see <see cref="BasePriorityRule.IsDuplicate"/>),
/// reported pairwise with the partner index.</summary>
public sealed class DuplicateValidator : IRuleSetValidator
{
    public void Validate(RuleValidationContext context, IReadOnlyList<BasePriorityRule> rules, List<List<string>?> errors)
    {
        foreach (var list in context.RulesByType.Values)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    var (firstIndex, first) = list[i];
                    var (secondIndex, second) = list[j];

                    if (first.IsDuplicate(second))
                    {
                        errors[firstIndex] ??= [];
                        errors[secondIndex] ??= [];
                        errors[firstIndex]!.Add(ASMKeys.ValidationDuplicate.Translate(secondIndex + 1));
                        errors[secondIndex]!.Add(ASMKeys.ValidationDuplicate.Translate(firstIndex + 1));
                    }
                }
            }
        }
    }
}
