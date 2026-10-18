using System;
using System.Collections.Generic;
using Verse;

namespace ASM;

/// <summary>Adds the trait rule options to the add-priority-rule menu (concrete trait via the
/// flag picker, positive/negative general rules).</summary>
public sealed class TraitRuleOptions : IRuleOptionsSource
{
    public void AddRuleOptions(List<FloatMenuOption> options, Action<BasePriorityRule> addRule)
    {
        // Trait options only when ATS trait content is actually loaded.
        if (!AnimalTraitsAccess.HasAvailableTraits)
        {
            return;
        }

        options.Add(new FloatMenuOption(ASMKeys.CondAddTrait.Translate(),
            () => Find.WindowStack.Add(new Dialog_TraitFlagPicker(picked =>
            {
                // TODO: 1 option for 1 trait def
                foreach ((HediffDef def, bool has) in picked)
                {
                    addRule(new TraitPriorityRule { has = has, trait = def });
                }
            }))));

        // TODO: 1 option with TraitType.Both
        AddOption(options, ASMKeys.CondPositiveHas, () => new TraitGeneralPriorityRule { has = true, type = TraitType.Positive }, addRule);
        AddOption(options, ASMKeys.CondPositiveMissing, () => new TraitGeneralPriorityRule { has = false, type = TraitType.Positive }, addRule);
        AddOption(options, ASMKeys.CondNegativeHas, () => new TraitGeneralPriorityRule { has = true, type = TraitType.Negative }, addRule);
        AddOption(options, ASMKeys.CondNegativeMissing, () => new TraitGeneralPriorityRule { has = false, type = TraitType.Negative }, addRule);
    }

    private static void AddOption(List<FloatMenuOption> options, string labelKey, Func<BasePriorityRule> factory, Action<BasePriorityRule> addRule) =>
        options.Add(new FloatMenuOption(labelKey.Translate(), () => addRule(factory())));
}
