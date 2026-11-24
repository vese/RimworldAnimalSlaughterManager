using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>The "Special rules" tab: the core's own keep and force-cull lists (always shown,
/// targets picked through the integrations' options), then the sections of every integration
/// source; sources with nothing to edit sink to the bottom as their notices.</summary>
public class SpecialSettingsTab(KindSettings settings) : BaseSettingsTab
{
    private readonly SpecialRulesListSection<KeepRule> keepSection =
        new(settings.specialRules.Keep, ASMKeys.KeepTraits, ASMKeys.KeepTraitsHelp, ASMKeys.PresetTitleKeep);

    private readonly SpecialRulesListSection<ForceCullRule> cullSection =
        new(settings.specialRules.ForceCull, ASMKeys.ForceCullTraits, ASMKeys.ForceCullTraitsHelp, ASMKeys.PresetTitleForceCull);

    private readonly List<(ISpecialSettingsSectionSource Source, ISpecialSettingsSection? Section)> sources =
        [.. IntegrationServices.GetServices<ISpecialSettingsSectionSource>()
            .OrderByDescending(source => source.Available)
            .Select(source => (source, source.Available ? source.CreateSection(settings) : null))];

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

    private void ResetTabSettings(ISettingsProvider comp, ThingDef animalDef, KindSettings settings)
    {
        settings.specialRules.Reset();

        foreach (var (_, section) in sources)
        {
            section?.Reset();
        }
    }

    private static void ResetAllSettings(ISettingsProvider comp, ThingDef animalDef, KindSettings settings)
    {
        settings.Reset(comp.GlobalSettings);
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings _settings, ISettingsProvider comp, ThingDef animalDef)
    {
        var top = y;
        var sourceSections = sources.Where(entry => entry.Section != null).ToList();
        var sectionCount = 2 + sourceSections.Count;

        // Even split of the tab height between the core lists and the integration sections.
        var gapSum = (sectionCount - 1) * 2 * UIConstants.GapY;
        var sectionHeight = MathF.Max(UIConstants.ListMinHeight, (contentHeight - gapSum) / sectionCount);

        top += keepSection.Draw(x, top, width, sectionHeight, comp, animalDef);

        top += sectionHeight + UIConstants.GapY;
        DrawDivider(x, top, width);
        top += UIConstants.GapY;

        top += cullSection.Draw(x, top, width, sectionHeight, comp, animalDef);

        foreach (var (_, section) in sourceSections)
        {
            top += sectionHeight + UIConstants.GapY;

            DrawDivider(x, top, width);

            top += UIConstants.GapY;

            top += section!.Draw(x, top, width, sectionHeight, comp, animalDef);
        }

        foreach (var (source, _) in sources.Where(entry => entry.Section == null && !entry.Source.Available))
        {
            top += UIConstants.GapY;
            top += DrawNotice(x, top, width, source.MissingModNotice);
        }
    }

    private static float DrawNotice(float x, float y, float width, TaggedString text)
    {
        var color = GUI.color;
        var font = Text.Font;
        GUI.color = Color.yellow;
        Text.Font = GameFont.Tiny;

        var height = Text.CalcHeight(text, width);
        Widgets.Label(new Rect(x, y, width, height), text);

        GUI.color = color;
        Text.Font = font;

        return height;
    }
}
