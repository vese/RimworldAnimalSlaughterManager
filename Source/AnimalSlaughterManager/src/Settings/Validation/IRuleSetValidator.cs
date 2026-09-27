using System.Collections.Generic;

namespace ASM;

/// <summary>Validates over the accumulated context: marks duplicated or covered rules, or any
/// other incompatibility, writing per-index problem messages into the errors list.</summary>
public interface IRuleSetValidator
{
    void Validate(RuleValidationContext context, List<List<string>?> errors);
}
