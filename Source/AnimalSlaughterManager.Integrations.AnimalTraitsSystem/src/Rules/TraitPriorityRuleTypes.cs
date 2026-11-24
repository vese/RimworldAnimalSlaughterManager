using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>The ATS rule types of the shared priority lists: the discriminators of the trait
/// rules, registered unconditionally so presets round-trip with the mod disabled (unresolved
/// defs are dropped on load).</summary>
public sealed class TraitPriorityRuleTypes : IPriorityRuleTypes
{
    public IEnumerable<(string DtoName, Func<BasePriorityRule> Factory)> Types =>
    [
        ("Trait", static () => new TraitPriorityRule()),
        ("TraitGeneral", static () => new TraitGeneralPriorityRule()),
    ];
}
