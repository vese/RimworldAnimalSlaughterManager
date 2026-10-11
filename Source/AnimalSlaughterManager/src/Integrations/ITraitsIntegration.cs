using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// The core's contract for an animal-traits mod integration. The rest of the mod talks to
/// <see cref="IntegrationRegistry.Traits"/> only — no direct references to the foreign mod's
/// wrappers outside the integration's own folder. The no-integration implementation answers
/// "nothing" to everything, so callers need no activity gates.
/// </summary>
public interface ITraitsIntegration
{
    bool Active { get; }

    bool HasAvailableTraits { get; }

    /// <summary>Trait defs the foreign mod currently provides.</summary>
    IReadOnlyList<HediffDef> KnownTraitDefs { get; }

    bool HasTrait(Pawn? pawn, HediffDef? def);

    bool IsInheritable(HediffDef def);

    /// <summary>Def names the foreign mod's own settings force-cull animals for.</summary>
    IReadOnlyCollection<string> CullTraitDefNames { get; }

    bool ShouldCull(Pawn pawn, IReadOnlyCollection<string> cullTraitDefNames);

    Color TraitColor(HediffDef def);

    string TraitTip(HediffDef def);

    string FormatStatFactor(float factor);

    /// <summary>Adds the integration's options to the add-rule menu; addRule puts the built
    /// rule into the target list.</summary>
    void AddRuleMenuOptions(List<FloatMenuOption> options, Action<BasePriorityRule> addRule);

    /// <summary>The per-kind tab with the integration's special rules; null = none.</summary>
    IKindSlaughterSettingsDialogTab? CreateSpecialRulesTab(KindSettings settings);
}
