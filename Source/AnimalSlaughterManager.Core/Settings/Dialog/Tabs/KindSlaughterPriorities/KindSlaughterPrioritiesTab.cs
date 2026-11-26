
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

public class KindSlaughterPrioritiesTab(ISettingsProvider comp, ThingDef animalDef, KindSettings settings) : BaseSettingsTab
{
    private const int ListsCountInRow = 2;

    private static readonly float ListsRowsCount = Mathf.Ceil(KindPrioritySettings.keys.Count / (float)ListsCountInRow);
    private static readonly float ListsRowsGapYSum = (ListsRowsCount - 1) * 2 * UIConstants.GapY;

    private static List<BasePriorityRule>? clipboard;

    public override TaggedString Name => ASMKeys.TabPriorities.Translate();

    protected override List<(string Text, Action<ISettingsProvider, ThingDef, KindSettings> Action)> HeaderButtons { get; } =
    [
        (ASMKeys.KindPresets, OpenKindPresetsWindow),
        (ASMKeys.ResetPriorities, ResetTabSettings),
        (ASMKeys.ResetKind, ResetAllSettings),
    ];

    private bool HasClipboard => clipboard != null && clipboard.Count > 0;

    private readonly PreferenceSettingsPanel preferenceSettingsPanel = new(settings.preferenceSettings, settings.preferenceSettings.Set, ASMKeys.PriorityHelp);

    private readonly List<ReorderableListState> listsStates = [.. KindPrioritySettings.keys.Select(x => new ReorderableListState())];

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings _settings, ISettingsProvider _comp, ThingDef _animalDef)
    {
        var top = preferenceSettingsPanel.Draw(x, y, width);

        top += UIConstants.GapY;

        top = DrawDoubleDivider(x, top, width);

        top += UIConstants.GapY;

        var conditionsSectionsHeight = contentHeight - (top - y);
        
        DrawConditionSections(x, top, width, conditionsSectionsHeight);
    }

    private void DrawConditionSections(float x, float y, float width, float contentHeight)
    {
        var top = y;
        var gapBetweenLists = 2 * UIConstants.GapX;
        var listWidth = (width - (ListsCountInRow - 1) * gapBetweenLists) / ListsCountInRow;
        var listHeight = MathF.Max(UIConstants.ListMinHeight, (contentHeight - ListsRowsGapYSum) / ListsRowsCount);
        var verticalDividerTop = top;

        for (var i = 0; i < KindPrioritySettings.keys.Count; i += ListsCountInRow)
        {
            if (i > 0)
            {
                top += listHeight + UIConstants.GapY;

                DrawDivider(x, top, width);

                top += UIConstants.GapY;
            }

            var left = x;

            for (var column = 0; column < ListsCountInRow && i + column < KindPrioritySettings.keys.Count; column++)
            {
                var key = KindPrioritySettings.keys[i + column];
                var title = KindPrioritySettings.ruleSetsNames[key].Translate();
                var setting = settings.prioritySettings.Get(key.Male, key.Adult);
                var listState = listsStates[i + column];
                var validation = settings.prioritySettings.Validate(key.Male, key.Adult);

                DrawConditionSection(left, top, listWidth, listHeight, title, setting, ref listState, key.Male, key.Adult, validation);

                left += listWidth + gapBetweenLists;
            }
        }

        for (var column = 1; column < ListsCountInRow; column++)
        {
            DrawVerticalDivider(x + column * (listWidth + gapBetweenLists) - UIConstants.GapX, verticalDividerTop, contentHeight);
        }
    }

    private void DrawConditionSection(float x, float y, float listWidth, float listHeight, TaggedString title,
        List<BasePriorityRule> list, ref ReorderableListState listState, bool male, bool adult, List<List<string>> validation)
    {
        var left = x;
        var top = y;

        // Row 1: title (left) + copy + paste icons right after the title.
        top += DrawConditionSectionHeader(x, y, title, list, male, adult);

        // Row 2: add, clear, presets (left-aligned).
        top += UIConstants.GapY;

        top += DrawButtons(left, top, list, male, adult);

        // List
        top += UIConstants.GapY;

        DrawConditionList(left, top, listWidth, listHeight, list, ref listState, male, adult, validation);
    }

    private float DrawConditionSectionHeader(float x, float y, TaggedString title, List<BasePriorityRule> rules, bool male, bool adult)
    {
        var color = GUI.color;
        GUI.color = Color.white;

        (var titleWidth, var height) = DrawConditionSectionHeaderTitle(x, y, title);
        DrawConditionSectionHeaderButtons(x + titleWidth, y, height, rules, male, adult);

        GUI.color = color;

        return height;
    }

    private (float width, float height) DrawConditionSectionHeaderTitle(float x, float y, TaggedString title)
    {
        var font = Text.Font;
        var anchor = Text.Anchor;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperLeft;

        var rectWidth = Text.CalcSize(title).x + UIConstants.GapX;
        var rectHeight = Text.LineHeight + UIConstants.TextPaddingY;

        Widgets.Label(new Rect(x, y, rectWidth, rectHeight), title);

        Text.Font = font;
        Text.Anchor = anchor;

        return (rectWidth, rectHeight);
    }

    private void DrawConditionSectionHeaderButtons(float x, float y, float height, List<BasePriorityRule> list, bool male, bool adult)
    {
        var font = Text.Font;
        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;

        var left = x;
        var copyButtonRect = new Rect(left, y, height, height);

        if (Widgets.ButtonImage(copyButtonRect, TexButton.Copy, tooltip: ASMKeys.CopyConditions.Translate()))
        {
            clipboard = [.. list.Select(c => c.Clone())];
        }

        left += height + UIConstants.GapX;

        if (HasClipboard)
        {
            var pasteButtonRect = new Rect(left, y, height, height);

            if (Widgets.ButtonImage(pasteButtonRect, TexButton.Paste, tooltip: ASMKeys.PasteConditions.Translate()))
            {
                settings.prioritySettings.ReplaceAll(male, adult, clipboard!.Select(c => c.Clone()).ToList());
            }
        }

        Text.Font = font;
        Text.Anchor = anchor;
    }

    private float DrawButtons(float x, float y, List<BasePriorityRule> list, bool male, bool adult)
    {
        var color = GUI.color;
        var font = Text.Font;
        var anchor = Text.Anchor;
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        var left = x;
        var height = UIConstants.ButtonHeight;

        var addButtonText = ASMKeys.AddCondition.Translate();
        var addButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(addButtonText).x + UIConstants.ButtonPaddingX);
        var addBtn = new Rect(left, y, addButtonWidth, height);
        left += addButtonWidth + UIConstants.GapX;

        if (Widgets.ButtonText(addBtn, addButtonText))
        {
            settings.prioritySettings.OpenAddRuleMenu(male, adult, animalDef);
        }

        var presetButtonText = ASMKeys.CondPresets.Translate();
        var presetButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(presetButtonText).x + UIConstants.ButtonPaddingX);
        var presetBtn = new Rect(left, y, presetButtonWidth, height);
        left += presetButtonWidth + UIConstants.GapX;

        if (Widgets.ButtonText(presetBtn, presetButtonText))
        {
            Find.WindowStack.Add(new KindPrioritiesListPresetBrowserDialog((ASM_MapComp)comp, male, adult, settings.prioritySettings));
        }

        var clearButtonText = ASMKeys.ClearList.Translate();
        var clearButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(clearButtonText).x + UIConstants.ButtonPaddingX);
        var clearBtn = new Rect(left, y, clearButtonWidth, height);

        GUI.enabled = list.Count > 0;

        if (Widgets.ButtonText(clearBtn, clearButtonText))
        {
            settings.prioritySettings.Clear(male, adult);
        }

        GUI.enabled = true;

        GUI.color = color;
        Text.Font = font;
        Text.Anchor = anchor;

        return height;
    }

    private void DrawConditionList(float x, float y, float listWidth, float listHeight,
        List<BasePriorityRule> list, ref ReorderableListState listState, bool male, bool adult, List<List<string>> validation)
    {
        ReorderableList.Draw(x, y, listWidth, listHeight, list.Count, listState,
            (from, to) => settings.prioritySettings.Move(male, adult, from, to),
            (row, i) => DrawConditionRow(row, list, i, validation, male, adult));
    }

    private void DrawConditionRow(Rect row, List<BasePriorityRule> list, int index, List<List<string>> validation, bool male, bool adult)
    {
        var rowValidation = validation[index];

        if (rowValidation is not null && rowValidation.Count > 0)
        {
            var prevColor = GUI.color;
            GUI.color = UIConstants.ValidationProblemTint;
            GUI.DrawTexture(row, BaseContent.WhiteTex);
            GUI.color = prevColor;
            TooltipHandler.TipRegion(row, string.Join("\n", rowValidation));
        }

        var left = row.x;
        var top = row.y;

        left += ReorderableList.DrawGrip(left, top, row.height);
        left += UIConstants.GapX;

        var condition = list[index];
        var extraDropdown = condition.GetExtraDropdown();

        // Right-side icon group: copy, warning, remove; the extra dropdown sits after the label.
        var extrasWidth = extraDropdown != null ? UIConstants.GapX + extraDropdown.Width : 0f;

        var labelWidth = row.width - UIConstants.IconSize - UIConstants.IconSize - UIConstants.IconSize - UIConstants.IconSize - extrasWidth;

        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = Color.white;

        var buttonHeight = UIConstants.ButtonHeight;
        top = row.y + (row.height - buttonHeight) / 2f;
        var labelButtonRect = new Rect(left, top, labelWidth, buttonHeight);

        Widgets.DrawHighlightIfMouseover(labelButtonRect);

        if (Widgets.ButtonInvisible(labelButtonRect))
        {
            settings.prioritySettings.ChangeVariant(male, adult, index);
        }

        var wrap = Text.WordWrap;
        Text.WordWrap = false;

        var labelRect = new Rect(
            labelButtonRect.x + ReorderableList.RowPaddingX,
            labelButtonRect.y,
            labelButtonRect.width - ReorderableList.RowPaddingX,
            labelButtonRect.height);
        Widgets.Label(labelRect, condition.Label);

        Text.WordWrap = wrap;

        Text.Anchor = TextAnchor.UpperLeft;

        TooltipHandler.TipRegion(labelButtonRect, ASMKeys.CondToggleTip.Translate());

        if (extraDropdown != null)
        {
            extraDropdown.Draw(labelButtonRect.xMax + UIConstants.GapX, top);
        }

        // Row-copy button — any rule can be duplicated.
        var copyRect = new Rect(
            row.xMax - UIConstants.GapX - UIConstants.IconSize - UIConstants.IconSize - UIConstants.IconSize - UIConstants.GapX,
            row.y + (row.height - UIConstants.IconSize) / 2f,
            UIConstants.IconSize,
            UIConstants.IconSize);
        TooltipHandler.TipRegion(copyRect, ASMKeys.Copy.Translate());

        if (Widgets.ButtonImage(copyRect, TexButton.Copy))
        {
            settings.prioritySettings.CopyAt(male, adult, index);
        }

        // Warning icon for problematic conditions.
        if (rowValidation is not null && rowValidation.Count > 0)
        {
            var warnRect = new Rect(
                row.xMax - UIConstants.GapX - UIConstants.IconSize - UIConstants.IconSize,
                row.y + (row.height - UIConstants.IconSize) / 2,
                UIConstants.IconSize,
                UIConstants.IconSize);

            GUI.color = Color.yellow;

            GUI.DrawTexture(warnRect, TexButton.Info);

            GUI.color = Color.white;

            var tipText = ASMKeys.CondProblemTip.Translate(condition.Label);

            TooltipHandler.TipRegion(warnRect, tipText);
        }

        top = row.y + (row.height - UIConstants.IconSize) / 2f;
        left = row.xMax - UIConstants.IconSize;
        var removeButtonRect = new Rect(left, top, UIConstants.IconSize, UIConstants.IconSize);

        if (ReorderableList.RemoveButton(removeButtonRect, ASMKeys.RemoveTrait))
        {
            settings.prioritySettings.RemoveAt(male, adult, index);
        }
    }

    private static void OpenKindPresetsWindow(ISettingsProvider comp, ThingDef animalDef, KindSettings _)
    {
        Find.WindowStack.Add(new KindSettingsPresetBrowserDialog((ASM_MapComp)comp, animalDef));
    }

    private static void ResetTabSettings(ISettingsProvider comp, ThingDef animalDef, KindSettings settings)
    {
        settings.preferenceSettings.Reset(comp.GlobalSettings.preferenceSettings);
        settings.prioritySettings.Reset();
    }

    private static void ResetAllSettings(ISettingsProvider comp, ThingDef animalDef, KindSettings settings)
    {
        settings.Reset(comp.GlobalSettings);
    }

}
