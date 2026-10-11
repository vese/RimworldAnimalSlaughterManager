using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// The integration registry — the extension point of the core: every integration adds its
/// service implementations here (from its own [StaticConstructorOnStartup] registration, so
/// even a foreign mod can register an integration with this mod). The core consumes the
/// registered implementations through the contracts only, never through concrete types.
/// </summary>
public static class IntegrationRegistry
{
    private static readonly List<IAnimalContentIntegration> animalContentIntegrations = [];

    /// <summary>All registered traits integrations combined; the no-integration behavior when
    /// the list is empty. The aggregate implements the same contract, so callers never care
    /// how many are registered.</summary>
    public static IAnimalContentIntegration AnimalContent => new AnimalContentAggregate(animalContentIntegrations);

    private static readonly List<IKindIntegrationSettingsProvider> kindSettingsProviders = [];

    /// <summary>Providers of integration-owned kind settings, registered unconditionally
    /// (the settings survive their mod being disabled).</summary>
    public static IReadOnlyList<IKindIntegrationSettingsProvider> KindSettingsProviders => kindSettingsProviders;

    public static void RegisterAnimalContent(IAnimalContentIntegration implementation)
    {
        animalContentIntegrations.Add(implementation);
    }

    public static void RegisterKindSettings(IKindIntegrationSettingsProvider provider)
    {
        if (kindSettingsProviders.All(p => p.KindSettingsKey != provider.KindSettingsKey))
        {
            kindSettingsProviders.Add(provider);
        }
    }
}

/// <summary>Combines every registered traits integration under one contract: set-like
/// services union, menu/selection services delegate to all, single-answer services answer
/// "any". With nothing registered this is the no-integration behavior.</summary>
public sealed class AnimalContentAggregate : IAnimalContentIntegration
{
    private readonly IReadOnlyList<IAnimalContentIntegration> items;

    public AnimalContentAggregate(IReadOnlyList<IAnimalContentIntegration> items)
    {
        this.items = items;
    }

    public bool Active => items.Any(i => i.Active);

    public bool HasAvailableTraits => items.Any(i => i.HasAvailableTraits);

    public IReadOnlyList<HediffDef> KnownTraitDefs => items.SelectMany(i => i.KnownTraitDefs).ToList();

    public bool HasTrait(Pawn? pawn, HediffDef? def) => items.Any(i => i.HasTrait(pawn, def));

    public bool IsInheritable(HediffDef def) => items.Any(i => i.IsInheritable(def));

    public IReadOnlyCollection<string> CullTraitDefNames => items.SelectMany(i => i.CullTraitDefNames).ToHashSet();

    public bool ShouldCull(Pawn pawn, IReadOnlyCollection<string> cullTraitDefNames) =>
        items.Any(i => i.ShouldCull(pawn, cullTraitDefNames));

    public Color TraitColor(HediffDef def) => items.FirstOrDefault(i => i.Active)?.TraitColor(def) ?? Color.white;

    public string TraitTip(HediffDef def) => items.FirstOrDefault(i => i.Active)?.TraitTip(def) ?? def.LabelCap.ToString();

    public string FormatStatFactor(float factor) => items.FirstOrDefault(i => i.Active)?.FormatStatFactor(factor) ?? factor.ToString("0%");

    public void AddRuleMenuOptions(List<FloatMenuOption> options, Action<BasePriorityRule> addRule)
    {
        foreach (var item in items)
        {
            item.AddRuleMenuOptions(options, addRule);
        }
    }

    public IKindSlaughterSettingsDialogTab? CreateSpecialRulesTab(KindSettings settings) =>
        items.Select(i => i.CreateSpecialRulesTab(settings)).FirstOrDefault(tab => tab != null);

    public IReadOnlyCollection<Pawn> SelectKeepReserved(Map map, ThingDef kind, KindSettings ks) =>
        items.SelectMany(i => i.SelectKeepReserved(map, kind, ks)).ToList();

    public int CountKept(Map map, ThingDef kind, KindSettings ks) =>
        items.Sum(i => i.CountKept(map, kind, ks));

    public bool ShouldForceCullBySettings(Pawn pawn, KindSettings ks) =>
        items.Any(i => i.ShouldForceCullBySettings(pawn, ks));

    public bool HasKeepRules(KindSettings ks) => items.Any(i => i.HasKeepRules(ks));

    public bool HasForceCullRules(KindSettings ks) => items.Any(i => i.HasForceCullRules(ks));
}
