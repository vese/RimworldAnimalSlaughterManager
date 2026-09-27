using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

/// <summary>
/// Event-sourced closure inside each trait set. Set rules are visited in list order; every rule
/// closes the set states it matches (recording the closing rule index). A rule is redundant when
/// all its states were already closed by earlier rules of the same set. A rule that closes the
/// last open state of a set exhausts it — every animal matches some rule of the set now, so any
/// rule placed below it (of any set) can never fire; that closing rule is marked as the one to
/// fix.
/// </summary>
public sealed class SetClosureValidator : IRuleSetValidator
{
    public void Validate(RuleValidationContext context, List<List<string>?> errors)
    {
        foreach (var set in context.Sets)
        {
            var states = set.EnumerateStates();
            // Closing rule index per state; -1 = open.
            var closedBy = new int[states.Count];
            for (int i = 0; i < closedBy.Length; i++)
            {
                closedBy[i] = -1;
            }

            var setRules = set.Rules.Values
                .SelectMany(list => list)
                .OrderBy(entry => entry.index)
                .ToList();

            foreach (var (index, rule) in setRules)
            {
                var open = new List<int>();

                for (int s = 0; s < states.Count; s++)
                {
                    if (closedBy[s] < 0 && set.Matches(rule, states[s]))
                    {
                        open.Add(s);
                    }
                }

                if (open.Count == 0)
                {
                    // Every state this rule matches is already closed — it never fires.
                    if (errors[index] == null)
                    {
                        var closers = closedBy
                            .Where(c => c >= 0)
                            .Select(c => c + 1)
                            .Distinct()
                            .OrderBy(n => n);

                        errors[index] ??= [];
                        errors[index]!.Add(ASMKeys.ValidationRedundant.Translate(string.Join(", ", closers)));
                    }

                    continue;
                }

                foreach (var s in open)
                {
                    closedBy[s] = index;
                }

                // The rule closed the last open state: the set (and with it the whole animal
                // space — every animal matches some rule of this set) is exhausted here.
                if (closedBy.All(c => c >= 0))
                {
                    errors[index] ??= [];
                    errors[index]!.Add(ASMKeys.ValidationExhausts.Translate());
                }
            }
        }
    }
}
