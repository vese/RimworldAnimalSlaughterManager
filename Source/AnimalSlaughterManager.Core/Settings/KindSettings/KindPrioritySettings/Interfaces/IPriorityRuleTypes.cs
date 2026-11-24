using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>The rule types contributed to the shared priority lists: DTO discriminators with
/// factories, so preset files deserialize into rules without the core knowing their types.</summary>
public interface IPriorityRuleTypes
{
    IEnumerable<(string DtoName, Func<BasePriorityRule> Factory)> Types { get; }
}
