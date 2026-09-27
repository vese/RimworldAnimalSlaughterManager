using System.Collections.Generic;

namespace ASM;

/// <summary>Validates accumulated trait sets: marks meaningless or duplicated rules, or any
/// other cross-type incompatibility, writing per-index problem messages into the errors list.</summary>
public interface IRuleSetValidator
{
    void Validate(RuleValidationContext context, int ruleCount, List<List<string>?> errors);
}
