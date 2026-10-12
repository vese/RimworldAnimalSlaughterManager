using System;
using System.Collections.Generic;
using Verse;

namespace ASM;

/// <summary>Contributes options to the add-priority-rule menu: the integration builds its own
/// rules and hands them to <paramref name="addRule"/>; nothing registered — no options.</summary>
public interface IRuleOptionsSource
{
    void AddRuleOptions(List<FloatMenuOption> options, Action<BasePriorityRule> addRule);
}
