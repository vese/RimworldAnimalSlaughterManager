using System.Collections.Generic;

namespace ASM;

/// <summary>
/// Closure inside each trait set, event-sourced over axis states instead of enumerated def
/// combinations: a rule is redundant when every axis state it matches was already closed by
/// earlier rules of the same set, and a rule that closes the last open state exhausts the set —
/// every animal matches some rule of the set now, so any rule placed below it (of any set) can
/// never fire. The sets themselves hold the axis logic; this validator only dispatches to them.
/// </summary>
public sealed class SetClosureValidator : IRuleSetValidator
{
    public void Validate(RuleValidationContext context, List<List<string>?> errors)
    {
        foreach (var set in context.Sets)
        {
            set.ValidateClosure(errors);
        }
    }
}
