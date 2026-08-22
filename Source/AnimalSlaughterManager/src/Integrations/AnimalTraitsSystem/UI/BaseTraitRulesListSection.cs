using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

public abstract class BaseTraitRulesListSection<T>(IEditableTraitsRuleSet<T> ruleSet) : ITraitRulesListSection<T> where T : ITraitRule
{
    protected IEditableTraitsRuleSet<T> RuleSet => ruleSet;

    protected abstract string TitleKey { get; }

    protected abstract string HelpKey { get; }

    public float Draw(float x, float y, float listWidth, float listHeight, ref ListSectionState listState, ASM_MapComp comp, ThingDef animalDef)
    {
        // Row 1: title.
        var left = x;
        var top = y;
        var titleHeight = UIConstants.MediumTextHeight;

        GUI.color = Color.white;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;

        Widgets.Label(new Rect(x, top, listWidth, titleHeight), TitleKey.Translate());

        top += titleHeight;

        // Row 2: add, clear, presets (left-aligned).
        var addButtonText = ASMKeys.AddTrait.Translate();
        var addButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(addButtonText).x + UIConstants.ButtonPaddingX);
        var presetButtonText = ASMKeys.TraitListPresets.Translate();
        var presetButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(presetButtonText).x + UIConstants.ButtonPaddingX);
        var clearButtonText = ASMKeys.ClearList.Translate();
        var clearButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(clearButtonText).x + UIConstants.ButtonPaddingX);

        Rect addBtn = new Rect(left, top, addButtonWidth, UIConstants.ButtonHeight);
        left += addButtonWidth + UIConstants.GapX;
        Rect presetBtn = new Rect(left, top, presetButtonWidth, UIConstants.ButtonHeight);
        left += presetButtonWidth + UIConstants.GapX;
        Rect clearBtn = new Rect(left, top, clearButtonWidth, UIConstants.ButtonHeight);

        if (Widgets.ButtonText(addBtn, addButtonText))
        {
            Find.WindowStack.Add(new Dialog_TraitPicker(RuleSet.Add));
        }

        if (Widgets.ButtonText(presetBtn, presetButtonText))
        {
            Find.WindowStack.Add(new Dialog_PresetBrowser<T>(comp, PresetScope.List, animalDef, ruleSet));
        }

        GUI.enabled = ruleSet.HasRules;

        if (Widgets.ButtonText(clearBtn, clearButtonText))
        {
            ruleSet.Clear();
            comp.MarkDirty();
        }

        GUI.enabled = true;

        top += UIConstants.ButtonHeight + UIConstants.GapY;

        // Row 3: help
        var help = HelpKey.Translate();
        var helpHeight = Text.CalcHeight(help, listWidth);
        top += UIConstants.GapY;

        GUI.color = Color.gray;
        Text.Font = GameFont.Tiny;

        Widgets.Label(new Rect(left, top, listWidth, helpHeight), help);

        top += helpHeight + UIConstants.GapY;

        // Row 4: table header
        top += DrawHeader(top, left, listWidth) + UIConstants.GapY;

        // List
        left = x;

        GUI.color = Color.white;
        Text.Font = GameFont.Small;

        var outRect = new Rect(left, top, listWidth, listHeight);
        var view = new Rect(left, top, listWidth - UIConstants.ScrollbarWidth, outRect.height/*TODO: need this? Mathf.Max(listState.contentHeight, outRect.height)*/);

        Widgets.BeginScrollView(outRect, ref listState.scroll, view);

        if (Event.current.type == EventType.Repaint)
        {
            listState.group = ReorderableWidget.NewGroup(ruleSet.Swap, ReorderableDirection.Vertical, outRect);
        }

        for (int i = 0; i < ruleSet.Count; i++)
        {
            var row = new Rect(view.x, top, view.width, UIConstants.MediumTextHeight);

            if (i % 2 == 1)
            {
                Widgets.DrawAltRect(row);
            }

            ReorderableWidget.Reorderable(listState.group, new Rect(row.x, row.y, UIConstants.IconSize, row.height));

            DrawRow(row, i, comp);

            top += row.height + KindSlaughterSettingsTabListHelper.ListGapY;
        }

        //TODO: need this?
        //listState.contentHeight = Mathf.Max(cy - y, outRect.height);

        Widgets.EndScrollView();

        return listHeight;
    }

    protected abstract float DrawHeader(float x, float y, float width);

    protected abstract float DrawRow(Rect row, int index, ASM_MapComp comp);

    protected void ReplaceTraits(int i, List<HediffDef> picked, ASM_MapComp comp)
    {
        if (picked is null || picked.Count == 0)
        {
            return;
        }

        RuleSet.ReplaceAt(i, picked);
        comp.MarkDirty();
    }
}
