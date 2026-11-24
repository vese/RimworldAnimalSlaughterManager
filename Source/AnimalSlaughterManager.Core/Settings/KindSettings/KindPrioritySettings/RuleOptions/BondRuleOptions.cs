using System;
using System.Collections.Generic;
using Verse;

namespace ASM;

/// <summary>Bond rule options of the add-rule menu.</summary>
public sealed class BondRuleOptions : IRuleOptionsSource
{
    public void AddRuleOptions(List<FloatMenuOption> options, bool male, bool adult, ThingDef animalDef, Action<BasePriorityRule> addRule)
    {
        // TODO: 1 option
        AddOption(options, ASMKeys.CondBondHas, () => new BondPriorityRule { has = true }, addRule);
        AddOption(options, ASMKeys.CondBondMissing, () => new BondPriorityRule { has = false }, addRule);
    }

    private static void AddOption(List<FloatMenuOption> options, string labelKey, Func<BasePriorityRule> factory, Action<BasePriorityRule> addRule)
    {
        options.Add(new FloatMenuOption(labelKey.Translate(), () => addRule(factory())));
    }
}
