using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>The shared "Special rules" tab: sections from every integration source, stacked
/// vertically; sources whose mod is missing contribute only their notices, drawn below.</summary>
public class SpecialSettingsTab(KindSettings settings) : BaseSettingsTab
{
    // Ready sections in registration order; null results of CreateSection are dropped here.
    private readonly List<ISpecialSettingsSection> sections =
        [.. ServiceRegistry.GetServices<ISpecialSettingsSectionSource>()
            .Where(source => source.Available)
            .Select(source => source.CreateSection(settings))
            .Where(section => section != null)
            .Select(x => x!)];

    // One notice per source whose mod is missing.
    private readonly List<TaggedString> missingModNotices =
        [.. ServiceRegistry.GetServices<ISpecialSettingsSectionSource>()
            .Where(source => !source.Available)
            .Select(source => source.MissingModNotice)];

    public override TaggedString Name => ASMKeys.TabExceptions.Translate();

    protected override List<(string Text, Action<ISettingsProvider, ThingDef, KindSettings> Action)> HeaderButtons =>
    [
        (ASMKeys.KindPresets, OpenKindPresetsWindow),
        (ASMKeys.ResetSpecialRules, ResetTabSettings),
        (ASMKeys.ResetKind, ResetAllSettings),
    ];

    private static void OpenKindPresetsWindow(ISettingsProvider comp, ThingDef animalDef, KindSettings _)
    {
        Find.WindowStack.Add(new KindSettingsPresetBrowserDialog(animalDef, comp.GetSettings(animalDef)));
    }

    private void ResetTabSettings(ISettingsProvider comp, ThingDef animalDef, KindSettings settings)
    {
        foreach (var section in sections)
        {
            section.Reset();
        }
    }

    private static void ResetAllSettings(ISettingsProvider comp, ThingDef animalDef, KindSettings settings)
    {
        settings.Reset(comp.GlobalSettings);
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings _settings, ISettingsProvider comp, ThingDef animalDef)
    {
        var top = y;

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

                top += sections[i].Draw(x, top, width, sectionHeight, comp, animalDef);
            }
        }

        foreach (var notice in missingModNotices)
        {
            top += UIConstants.GapY;
            top += DrawNotice(x, top, width, notice);
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
