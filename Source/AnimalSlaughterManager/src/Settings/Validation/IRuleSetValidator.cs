using System;

namespace ASM;

/// <summary>Validates over the accumulated context: marks duplicated or covered rules, or any
/// other incompatibility, reporting a problem message for a rule list index.</summary>
public interface IRuleSetValidator
{
    void Validate(RuleValidationContext context, Action<int, string> addError);
}
