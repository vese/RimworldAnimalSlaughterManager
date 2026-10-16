using Verse;
using System;

namespace ASM;

/// <summary>Pairwise coverage with list order: for every pair where a <typeparamref name="TRule"/>
/// stands above a <typeparamref name="TOther"/> and covers it, the lower rule never fires (first
/// match wins) and is marked redundant. TRule may equal TOther for same-type coverage.</summary>
public sealed class CoverageValidator<TRule, TOther> : IRuleSetValidator
    where TRule : BasePriorityRule, ICoversRule<TOther>
    where TOther : BasePriorityRule
{
    public void Validate(RuleValidationContext context, Action<int, string> addProblem)
    {
        if (!context.RulesByType.TryGetValue(typeof(TRule), out var covering) ||
            !context.RulesByType.TryGetValue(typeof(TOther), out var covered))
        {
            return;
        }

        foreach (var (upperIndex, upper) in covering)
        {
            foreach (var (lowerIndex, lower) in covered)
            {
                if (upperIndex < lowerIndex && ((ICoversRule<TOther>)upper).Covers((TOther)lower))
                {
                    addProblem(lowerIndex, ASMKeys.ValidationRedundant.Translate(upperIndex + 1));
                }
            }
        }
    }
}
