using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// The integration registry: the core asks it for integration services, never for foreign-mod
/// wrappers directly, and never names an integration by type. Integrations register themselves
/// at startup from their own folder (their [StaticConstructorOnStartup] registration class);
/// until then the default answers implement the no-integration behavior.
/// </summary>
public static class IntegrationRegistry
{
    private static ITraitsIntegration traits = new NoneTraitsIntegration();

    public static ITraitsIntegration Traits => traits;

    public static void RegisterTraits(ITraitsIntegration implementation)
    {
        traits = implementation;
    }
}

/// <summary>The no-integration behavior: no traits, no culling, no UI; callers need no gates.</summary>
public sealed class NoneTraitsIntegration : ITraitsIntegration
{
    public bool Active => false;

    public bool HasAvailableTraits => false;

    public IReadOnlyList<HediffDef> KnownTraitDefs => Array.Empty<HediffDef>();

    public bool HasTrait(Pawn? pawn, HediffDef? def) => false;

    public bool IsInheritable(HediffDef def) => false;

    public IReadOnlyCollection<string> CullTraitDefNames => Array.Empty<string>();

    public bool ShouldCull(Pawn pawn, IReadOnlyCollection<string> cullTraitDefNames) => false;

    public Color TraitColor(HediffDef def) => Color.white;

    public string TraitTip(HediffDef def) => def.LabelCap.ToString();

    public string FormatStatFactor(float factor) => factor.ToString("0%");

    public void AddRuleMenuOptions(List<FloatMenuOption> options, Action<BasePriorityRule> addRule)
    {
    }

    public IKindSlaughterSettingsDialogTab? CreateSpecialRulesTab(KindSettings settings) => null;
}
