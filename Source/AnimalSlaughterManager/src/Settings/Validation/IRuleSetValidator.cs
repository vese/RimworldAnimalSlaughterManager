using System.Collections.Generic;

namespace ASM;

/// <summary>Validates the rule list over the accumulated context: marks duplicated or covered
/// rules, or any other incompatibility, writing per-index problem messages into the errors list.</summary>
public interface IRuleSetValidator
{
    void Validate(RuleValidationContext context, IReadOnlyList<BasePriorityRule> rules, List<List<string>?> errors);
}
