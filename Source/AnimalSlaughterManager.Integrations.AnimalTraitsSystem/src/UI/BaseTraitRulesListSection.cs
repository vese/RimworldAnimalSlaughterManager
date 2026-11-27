using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

public abstract class BaseTraitRulesListSection<T>(IEditableTraitsRuleSet<T> ruleSet) : ITraitRulesListSection<T> where T : ITraitRule
{
    protected IEditableTraitsRuleSet<T> RuleSet => ruleSet;

    protected abstract string TitleKey { get; }

    protected abstract string HelpKey { get; }

    protected abstract string PresetsTitleKey { get; }

    public float Draw(float x, float y, float listWidth, float listHeight, ref ReorderableListState listState, ISettingsProvider comp, ThingDef animalDef)
    {
        // Row 1: title.
        var left = x;
        var top = y;

        top += DrawTitle(left, top, listWidth);

        // Row 2: add, clear, presets (left-aligned).
        top += UIConstants.GapY;

        top += DrawButtons(left, top, comp, animalDef);

        // Row 3: help
        top += UIConstants.GapY;

        top += DrawHelp(left, top, listWidth);

        // Row 4: table header
        top += UIConstants.GapY;

        top += DrawHeader(left, top, listWidth);

        // List
        left = x;
        top += UIConstants.GapY;

        DrawList(left, top, listWidth, listHeight, ref listState);

        return listHeight;
    }

    private float DrawTitle(float x, float y, float width)
    {
        var color = GUI.color;
        var font = Text.Font;
        var anchor = Text.Anchor;
        GUI.color = Color.white;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperLeft;

        var height = Text.LineHeight + UIConstants.TextPaddingY;
        Widgets.Label(new Rect(x, y, width, height), TitleKey.Translate());

        GUI.color = color;
        Text.Font = font;
        Text.Anchor = anchor;

        return height;
    }

    private float DrawButtons(float x, float y, ISettingsProvider comp, ThingDef animalDef)
    {
        var color = GUI.color;
        var font = Text.Font;
        var anchor = Text.Anchor;
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        var left = x;
        var height = UIConstants.ButtonHeight;

        var addButtonText = ASMKeys.AddTrait.Translate();
        var addButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(addButtonText).x + UIConstants.ButtonPaddingX);
        var addBtn = new Rect(left, y, addButtonWidth, height);
        left += addButtonWidth + UIConstants.GapX;

        if (Widgets.ButtonText(addBtn, addButtonText))
        {
            Find.WindowStack.Add(new Dialog_TraitPicker(RuleSet.Add));
        }

        var presetButtonText = ASMKeys.TraitListPresets.Translate();
        var presetButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(presetButtonText).x + UIConstants.ButtonPaddingX);
        var presetBtn = new Rect(left, y, presetButtonWidth, height);
        left += presetButtonWidth + UIConstants.GapX;

        if (Widgets.ButtonText(presetBtn, presetButtonText))
        {
            Find.WindowStack.Add(new SpecialSettingsListPresetBrowserDialog(animalDef, PresetsTitleKey,
                name => PresetRepository.SaveList(name, PresetScope.SpecialList, RuleSet.PresetsFolder, RuleSet.ToPreset()),
                e => PresetRepository.ReadList(e) is { } dto && RuleSet.FromPreset(dto),
                () => RuleSet.HasRules));
        }

        var clearButtonText = ASMKeys.ClearList.Translate();
        var clearButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(clearButtonText).x + UIConstants.ButtonPaddingX);
        var clearBtn = new Rect(left, y, clearButtonWidth, height);

        GUI.enabled = ruleSet.HasRules;

        if (Widgets.ButtonText(clearBtn, clearButtonText))
        {
            ruleSet.Clear();
        }

        GUI.enabled = true;

        GUI.color = color;
        Text.Font = font;
        Text.Anchor = anchor;

        return height;
    }

    private float DrawHelp(float x, float y, float width)
    {
        var color = GUI.color;
        var font = Text.Font;
        GUI.color = Color.gray;
        Text.Font = GameFont.Tiny;

        var help = HelpKey.Translate();
        var height = Text.CalcHeight(help, width);

        Widgets.Label(new Rect(x, y, width, height), help);

        GUI.color = color;
        Text.Font = font;

        return height;
    }

    protected abstract float DrawHeader(float x, float y, float width);

    private void DrawList(float x, float y, float listWidth, float listHeight, ref ReorderableListState listState)
    {
        ReorderableList.Draw(x, y, listWidth, listHeight, ruleSet.Count, listState, ruleSet.Swap, DrawRow);
    }

    protected abstract void DrawRow(Rect row, int index);

    protected void ReplaceTraits(int i, List<HediffDef> picked)
    {
        if (picked is null || picked.Count == 0)
        {
            return;
        }

        RuleSet.ReplaceAt(i, picked);
    }
}
