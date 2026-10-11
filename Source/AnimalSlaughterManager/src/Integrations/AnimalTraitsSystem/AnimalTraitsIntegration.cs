using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// The Animal Traits System integration: adapts <see cref="AnimalTraitsAccess"/> (the foreign
/// mod's API wrapper) to <see cref="ITraitsIntegration"/>. Registered at startup when ATS is
/// active; nothing outside this folder references ATS wrappers.
/// </summary>
public sealed class AnimalTraitsIntegration : ITraitsIntegration
{
    public const string PackageId = "luved.animaltraits";

    public bool Active => AnimalTraitsAccess.IsATSActive;

    public bool HasAvailableTraits => AnimalTraitsAccess.HasAvailableTraits;

    public IReadOnlyList<HediffDef> KnownTraitDefs => AnimalTraitsAccess.KnownTraitDefs;

    public bool HasTrait(Pawn? pawn, HediffDef? def) => AnimalTraitsAccess.HasTrait(pawn, def);

    public bool IsInheritable(HediffDef def) => AnimalTraitsAccess.IsInheritable(def);

    public IReadOnlyCollection<string> CullTraitDefNames => AnimalTraitsAccess.GetCullTraitDefNames();

    public bool ShouldCull(Pawn pawn, IReadOnlyCollection<string> cullTraitDefNames) =>
        AnimalTraitsAccess.ShouldCull(pawn, cullTraitDefNames as HashSet<string> ?? [.. cullTraitDefNames]);

    public Color TraitColor(HediffDef def) => AnimalTraitsAccess.TraitColor(def);

    public string TraitTip(HediffDef def) => AnimalTraitsAccess.TraitTip(def);

    public string FormatStatFactor(float factor) => AnimalTraitsAccess.FormatStatFactor(factor);

    public void AddRuleMenuOptions(List<FloatMenuOption> options, Action<BasePriorityRule> addRule)
    {
        // Trait options only when ATS trait content is actually loaded.
        if (!HasAvailableTraits)
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

    public IKindSlaughterSettingsDialogTab? CreateSpecialRulesTab(KindSettings settings) =>
        new KindSlaughterSettingsDialogSpecialRulesTab(settings.traitsSettings);
}
