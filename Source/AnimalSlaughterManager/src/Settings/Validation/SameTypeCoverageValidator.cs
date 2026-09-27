using Verse;
using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>Coverage among rules of the same type, in list order: when an earlier rule covers a
/// later one, the later rule never fires (first match wins) and is redundant.</summary>
public sealed class SameTypeCoverageValidator : IRuleSetValidator
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

                    if (first.Covers(second) && !second.Covers(first))
                    {
                        errors[secondIndex] ??= [];
                        errors[secondIndex]!.Add(ASMKeys.ValidationRedundant.Translate(firstIndex + 1));
                    }
                }
            }
        }
    }
}
