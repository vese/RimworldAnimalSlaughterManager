using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

public class KindSlaughterSettingsDialogSpecialRulesTab(KindTraitsSettings traitsSettings) : BaseKindSlaughterSettingsTab
{
    private const int RuleSetsCount = 2;
    private const float RuleSetsGapYSum = (RuleSetsCount - 1) * 2 * UIConstants.GapY;

    private readonly ITraitRulesListSection<TraitProtectRule> protectEditor = traitsSettings.protectRuleSet.GetEditor();
    private readonly ITraitRulesListSection<TraitRule> forceCullEditor = traitsSettings.forceCullRuleSet.GetEditor();
    private ListSectionState protectEditorState = new();
    private ListSectionState forceCullEditorState = new();

    public override TaggedString Name => ASMKeys.TabExceptions.Translate();

    protected override List<(string Text, Action<ASM_MapComp, ThingDef, KindSettings> Action)> HeaderButtons { get; } =
    [
        (ASMKeys.KindPresets, OpenKindPresetsWindow),
        (ASMKeys.ResetSpecialRules, ResetTabSettings),
        (ASMKeys.ResetKind, ResetAllSettings),
    ];

    private static void OpenKindPresetsWindow(ASM_MapComp comp, ThingDef animalDef, KindSettings _)
    {
        Find.WindowStack.Add(new Dialog_PresetBrowser(comp, PresetScope.Kind, animalDef/*, null*/));
    }

    private static void ResetTabSettings(ASM_MapComp comp, ThingDef animalDef, KindSettings settings)
    {
        settings.traitsSettings.Reset();

        comp.MarkDirty();
    }

    private static void ResetAllSettings(ASM_MapComp comp, ThingDef animalDef, KindSettings settings)
    {
        settings.Reset(comp.globalSettings);

        comp.MarkDirty();
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings settings, ASM_MapComp comp, ThingDef animalDef)
    {
        // TODO: move in ats specific
        if (!AnimalTraitsAccess.HasAvailableTraits)
        {
            var color = GUI.color;
            var font = Text.Font;
            GUI.color = Color.yellow;
            Text.Font = GameFont.Tiny;

            var warningText = ASMKeys.ATSNotDetected.Translate();
            Widgets.Label(new Rect(x, y, width, Text.CalcHeight(warningText, width)), warningText);

            GUI.color = color;
            Text.Font = font;

            return;
        }

        var listHeight = MathF.Max(UIConstants.ListMinHeight, (contentHeight - RuleSetsGapYSum) / RuleSetsCount);
        var top = y;

        top += protectEditor.Draw(x, top, width, listHeight, ref protectEditorState, comp, animalDef);

        top += UIConstants.GapY;

        DrawDivider(x, top, width);

        top += UIConstants.GapY;

        forceCullEditor.Draw(x, top, width, listHeight, ref forceCullEditorState, comp, animalDef);
    }
}
