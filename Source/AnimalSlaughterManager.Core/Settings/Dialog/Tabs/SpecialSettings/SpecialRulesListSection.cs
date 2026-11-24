using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>One special-rules list of the special settings tab: title, buttons (add via the
/// integration's picker, clear, list presets), help, header and the reorderable rows.</summary>
public class SpecialRulesListSection<T>(SpecialRuleSet<T> ruleSet, string titleKey, string helpKey, string presetsTitleKey)
    where T : SpecialRule, new()
{
    private readonly ReorderableListState listState = new();

    public float Draw(float x, float y, float width, float height, ISettingsProvider comp, ThingDef animalDef)
    {
        var left = x;
        var top = y;

        top += DrawTitle(left, top, width);
        top += UIConstants.GapY;
        top += DrawButtons(left, top, comp, animalDef);
        top += UIConstants.GapY;
        top += DrawHelp(left, top, width);
        top += UIConstants.GapY;
        top += DrawHeader(left, top, width);
        top += UIConstants.GapY;

        ReorderableList.Draw(left, top, width, MathF.Max(height - (top - y), UIConstants.ListMinHeight), ruleSet.Count, listState, ruleSet.Swap, DrawRow);

        return height;
    }

    private static ISpecialRuleOptionsSource? Options() => IntegrationServices.GetServices<ISpecialRuleOptionsSource>().FirstOrDefault();

    private float DrawTitle(float x, float y, float width)
    {
        var color = GUI.color;
        var font = Text.Font;
        var anchor = Text.Anchor;
        GUI.color = Color.white;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperLeft;

        var height = Text.LineHeight + UIConstants.TextPaddingY;
        Widgets.Label(new Rect(x, y, width, height), titleKey.Translate());

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
        var options = Options();

        var addButtonText = ASMKeys.AddTrait.Translate();
        var addButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(addButtonText).x + UIConstants.ButtonPaddingX);
        var addBtn = new Rect(left, y, addButtonWidth, height);
        left += addButtonWidth + UIConstants.GapX;

        GUI.enabled = options is { Available: true };

        if (Widgets.ButtonText(addBtn, addButtonText))
        {
            options!.OpenPicker(picked => ruleSet.Add(picked));
        }

        GUI.enabled = true;

        if (options is { Available: false })
        {
            TooltipHandler.TipRegion(addBtn, options.MissingModNotice);
        }

        var presetButtonText = ASMKeys.TraitListPresets.Translate();
        var presetButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(presetButtonText).x + UIConstants.ButtonPaddingX);
        var presetBtn = new Rect(left, y, presetButtonWidth, height);
        left += presetButtonWidth + UIConstants.GapX;

        if (Widgets.ButtonText(presetBtn, presetButtonText))
        {
            IntegrationServices.GetServices<IPresetWindows>()[0].OpenListBrowser(comp, animalDef, presetsTitleKey.Translate(animalDef.LabelCap).ToString(),
                name => PresetRepository.ExportList(name, animalDef, ruleSet),
                e => PresetRepository.ReadKindSlice(e, animalDef) is { } kd && ruleSet.ReadXml(PresetSectionSerialization.Wrap(kd.IntegrationSections)),
                () => ruleSet.HasRules);
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

        var help = helpKey.Translate();
        var height = Text.CalcHeight(help, width);

        Widgets.Label(new Rect(x, y, width, height), help);

        GUI.color = color;
        Text.Font = font;

        return height;
    }

    private static float DrawHeader(float x, float y, float width)
    {
        var wrap = Text.WordWrap;
        var anchor = Text.Anchor;
        var font = Text.Font;
        var color = GUI.color;
        Text.WordWrap = false;
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        GUI.color = Color.white;

        var labelHeight = Text.LineHeight;
        var gripOffset = UIConstants.IconSize;
        var hasKeepCount = typeof(T) == typeof(KeepRule);
        var rightSideWidth = UIConstants.ButtonMinWidth + UIConstants.GapX + // keep
            UIConstants.ButtonMinWidth + UIConstants.GapX + // age
            UIConstants.ButtonMinWidth + UIConstants.GapX + // gender
            UIConstants.ButtonMinWidth + UIConstants.GapX + // inheritability
            UIConstants.IconSize + UIConstants.GapX + // copy
            UIConstants.IconSize; // remove
        var targetColumnWidth = width - gripOffset - rightSideWidth;
        var left = x + gripOffset;

        Widgets.Label(new Rect(left, y, targetColumnWidth, labelHeight), ASMKeys.Trait.Translate());
        left += targetColumnWidth;

        if (hasKeepCount)
        {
            Widgets.Label(new Rect(left, y, UIConstants.ButtonMinWidth, labelHeight), ASMKeys.Keep.Translate());
        }

        left += UIConstants.ButtonMinWidth + UIConstants.GapX;
        Widgets.Label(new Rect(left, y, UIConstants.ButtonMinWidth, labelHeight), ASMKeys.AgeScope.Translate());
        left += UIConstants.ButtonMinWidth + UIConstants.GapX;
        Widgets.Label(new Rect(left, y, UIConstants.ButtonMinWidth, labelHeight), ASMKeys.GenderScope.Translate());
        left += UIConstants.ButtonMinWidth + UIConstants.GapX;
        Widgets.Label(new Rect(left, y, UIConstants.ButtonMinWidth, labelHeight), ASMKeys.InheritMode.Translate());

        Text.WordWrap = wrap;
        Text.Anchor = anchor;
        Text.Font = font;
        GUI.color = color;

        return labelHeight;
    }

    private void DrawRow(Rect row, int index)
    {
        var rule = ruleSet.Get(index);

        if (rule.target is null)
        {
            return;
        }

        var options = Options();
        var anchor = Text.Anchor;
        var left = row.x;
        var top = row.y;

        left += ReorderableList.DrawGrip(left, top, row.height);

        var right = row.xMax - (UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.IconSize + UIConstants.GapX +
            UIConstants.IconSize);
        var targetButtonWidth = right - left;
        var buttonHeight = UIConstants.ButtonHeight;
        top = row.y + (row.height - buttonHeight) / 2f;

        left += SpecialRuleListButton.Draw(new Rect(left, top, targetButtonWidth, buttonHeight), rule, options,
            () => options?.OpenPicker(picked => ruleSet.ReplaceAt(index, picked)));

        Text.Anchor = TextAnchor.MiddleCenter;

        if (rule is KeepRule keep)
        {
            var keepCountValue = Widgets.TextField(new Rect(left, top, UIConstants.ButtonMinWidth, buttonHeight), keep.keepCount.ToString());

            if (int.TryParse(keepCountValue, out int n) && n >= 0 && n != keep.keepCount)
            {
                keep.SetKeepCount(n);
            }
        }

        Text.Anchor = TextAnchor.UpperLeft;

        left += UIConstants.ButtonMinWidth + UIConstants.GapX;

        left += Dropdown.DrawEnum(left, top, UIConstants.ButtonMinWidth, rule.ageScope, val => val.Translate(), val => rule.SetAgeScope(val)) + UIConstants.GapX;
        left += Dropdown.DrawEnum(left, top, UIConstants.ButtonMinWidth, rule.genderScope, val => val.Translate(), val => rule.SetGenderScope(val)) + UIConstants.GapX;
        left += DrawInheritDropdown(left, top, rule, options) + UIConstants.GapX;

        top = row.y + (row.height - UIConstants.IconSize) / 2f;
        var copyButtonRect = new Rect(left, top, UIConstants.IconSize, UIConstants.IconSize);

        if (ReorderableList.CopyButton(copyButtonRect, ASMKeys.Copy))
        {
            ruleSet.CopyAt(index);
        }

        left += UIConstants.IconSize + UIConstants.GapX;
        var removeButtonRect = new Rect(left, top, UIConstants.IconSize, UIConstants.IconSize);

        if (ReorderableList.RemoveButton(removeButtonRect, ASMKeys.RemoveTrait))
        {
            ruleSet.RemoveAt(index);
        }

        Text.Anchor = anchor;
    }

    private static float DrawInheritDropdown(float x, float y, SpecialRule rule, ISpecialRuleOptionsSource? options)
    {
        if (options == null || options.InheritModes.Count == 0)
        {
            return UIConstants.ButtonMinWidth;
        }

        var current = options.InheritModes.Contains(rule.inheritMode) ? rule.inheritMode : options.InheritModes[0];

        return Dropdown.Draw(x, y, UIConstants.ButtonMinWidth, options.LabelOfInheritMode(current),
            Dropdown.Options(options.InheritModes.ToList(), options.LabelOfInheritMode, rule.SetInheritMode));
    }
}
