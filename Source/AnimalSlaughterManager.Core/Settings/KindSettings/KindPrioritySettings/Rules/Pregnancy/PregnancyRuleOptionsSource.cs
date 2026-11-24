using System;
using System.Collections.Generic;
using Verse;

namespace ASM;

/// <summary>Pregnancy rule options of the add-rule menu; only the adult-female bucket can
/// hold them.</summary>
public sealed class PregnancyRuleOptionsSource : IRuleOptionsSource
{
    public void AddRuleOptions(List<FloatMenuOption> options, bool male, bool adult, ThingDef animalDef, Action<BasePriorityRule> addRule)
    {
        if (!male && adult)
        {
            // TODO: 1 option
            AddOption(options, ASMKeys.CondPregnantHas, () => new PregnancyPriorityRule { has = true }, addRule);
            AddOption(options, ASMKeys.CondPregnantMissing, () => new PregnancyPriorityRule { has = false }, addRule);
        }
    }

    private static void AddOption(List<FloatMenuOption> options, string labelKey, Func<BasePriorityRule> factory, Action<BasePriorityRule> addRule)
    {
        options.Add(new FloatMenuOption(labelKey.Translate(), () => addRule(factory())));
    }
}
