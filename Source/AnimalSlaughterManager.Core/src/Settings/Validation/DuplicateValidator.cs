using Verse;
using System;

namespace ASM;

/// <summary>Duplicates: identical rules of the same type (see <see cref="BasePriorityRule.IsDuplicate"/>),
/// reported pairwise with the partner index.</summary>
public sealed class DuplicateValidator : IRuleSetValidator
{
    public void Validate(RuleValidationContext context, Action<int, string> addProblem)
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
                        addProblem(firstIndex, ASMKeys.ValidationDuplicate.Translate(secondIndex + 1));
                        addProblem(secondIndex, ASMKeys.ValidationDuplicate.Translate(firstIndex + 1));
                    }
                }
            }
        }
    }
}
