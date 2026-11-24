using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>The "Special rules" tab: the core's own keep and force-cull lists, always shown;
/// the targets to pick come from the integrations' ISpecialRuleOptionsSource.</summary>
public class SpecialSettingsTab(KindSettings settings) : BaseSettingsTab
{
    private readonly SpecialRulesListSection<KeepRule> keepSection =
        new(settings.specialRules.Keep, ASMKeys.KeepTraits, ASMKeys.KeepTraitsHelp, ASMKeys.PresetTitleKeep);

    private readonly SpecialRulesListSection<ForceCullRule> cullSection =
        new(settings.specialRules.ForceCull, ASMKeys.ForceCullTraits, ASMKeys.ForceCullTraitsHelp, ASMKeys.PresetTitleForceCull);

    public override TaggedString Name => ASMKeys.TabExceptions.Translate();

    protected override List<(string Text, Action<ISettingsProvider, ThingDef, KindSettings> Action)> HeaderButtons =>
    [
        (ASMKeys.KindPresets, OpenKindPresetsWindow),
        (ASMKeys.ResetSpecialRules, ResetTabSettings),
        (ASMKeys.ResetKind, ResetAllSettings),
    ];

    private static void OpenKindPresetsWindow(ISettingsProvider comp, ThingDef animalDef, KindSettings _)
    {
        IntegrationServices.GetServices<IPresetWindows>()[0].OpenKindBrowser(comp, animalDef);
    }

    private static void ResetTabSettings(ISettingsProvider comp, ThingDef animalDef, KindSettings settings)
    {
        settings.specialRules.Reset();
    }

    private static void ResetAllSettings(ISettingsProvider comp, ThingDef animalDef, KindSettings settings)
    {
        settings.Reset(comp.GlobalSettings);
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings _settings, ISettingsProvider comp, ThingDef animalDef)
    {
        var top = y;

        // Even split of the tab height between the two lists.
        var sectionHeight = MathF.Max(UIConstants.ListMinHeight, (contentHeight - 2 * UIConstants.GapY) / 2);

        top += keepSection.Draw(x, top, width, sectionHeight, comp, animalDef);

        top += sectionHeight + UIConstants.GapY;
        DrawDivider(x, top, width);
        top += UIConstants.GapY;

        cullSection.Draw(x, top, width, sectionHeight, comp, animalDef);
    }
}
