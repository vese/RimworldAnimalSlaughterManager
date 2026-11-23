using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>The shared "Special rules" tab: sections from every integration source, stacked
/// vertically; sources with nothing to edit sink to the bottom as their notices.</summary>
public class SpecialSettingsTab(KindSettings settings) : BaseSettingsTab
{
    private readonly List<(ISpecialSettingsSectionSource Source, ISpecialSettingsSection? Section)> sources =
        [.. IntegrationServices.GetServices<ISpecialSettingsSectionSource>()
            .OrderByDescending(source => source.Available)
            .Select(source => (source, source.Available ? source.CreateSection(settings) : null))];

    public override TaggedString Name => ASMKeys.TabExceptions.Translate();

    protected override List<(string Text, Action<IKindSettingsHost, ThingDef, KindSettings> Action)> HeaderButtons =>
    [
        (ASMKeys.KindPresets, OpenKindPresetsWindow),
        (ASMKeys.ResetSpecialRules, ResetTabSettings),
        (ASMKeys.ResetKind, ResetAllSettings),
    ];

    private static void OpenKindPresetsWindow(IKindSettingsHost comp, ThingDef animalDef, KindSettings _)
    {
        IntegrationServices.GetServices<IPresetWindows>()[0].OpenKindBrowser(comp, animalDef);
    }

    private void ResetTabSettings(IKindSettingsHost comp, ThingDef animalDef, KindSettings settings)
    {
        foreach (var (_, section) in sources)
        {
            section?.Reset();
        }
    }

    private static void ResetAllSettings(IKindSettingsHost comp, ThingDef animalDef, KindSettings settings)
    {
        settings.Reset(comp.GlobalSettings);
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings _settings, IKindSettingsHost comp, ThingDef animalDef)
    {
        var top = y;
        var sections = sources.Where(entry => entry.Section != null).ToList();

        if (sections.Count > 0)
        {
            // Even split of the tab height between the sections, as the trait lists did before
            // the tab became shared.
            var gapSum = (sections.Count - 1) * 2 * UIConstants.GapY;
            var sectionHeight = MathF.Max(UIConstants.ListMinHeight, (contentHeight - gapSum) / sections.Count);

            for (var i = 0; i < sections.Count; i++)
            {
                if (i > 0)
                {
                    top += sectionHeight + UIConstants.GapY;

                    DrawDivider(x, top, width);

                    top += UIConstants.GapY;
                }

                top += sections[i].Section!.Draw(x, top, width, sectionHeight, comp, animalDef);
            }
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
