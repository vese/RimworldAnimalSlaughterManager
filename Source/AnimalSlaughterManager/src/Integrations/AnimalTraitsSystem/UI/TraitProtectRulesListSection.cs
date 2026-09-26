using UnityEngine;
using Verse;

namespace ASM;

public class TraitProtectRulesListSection(IEditableTraitsRuleSet<TraitProtectRule> rules) : BaseTraitRulesListSection<TraitProtectRule>(rules)
{
    protected override string TitleKey { get; } = ASMKeys.KeepTraits;
    protected override string HelpKey { get; } = ASMKeys.KeepTraitsHelp;
    protected override string PresetsTitleKey { get; } = ASMKeys.PresetTitleKeep;

    protected override float DrawHeader(float x, float y, float width)
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
        var rightSideWidth = UIConstants.ButtonMinWidth + UIConstants.GapX + // keep
            UIConstants.ButtonMinWidth + UIConstants.GapX + // age
            UIConstants.ButtonMinWidth + UIConstants.GapX + // gender
            UIConstants.ButtonMinWidth + UIConstants.GapX + // inheritability
            UIConstants.IconSize + UIConstants.GapX + // copy
            UIConstants.IconSize; // remove
        var traitColumnWidth = width - gripOffset - rightSideWidth;
        var left = x + gripOffset;

        Widgets.Label(new Rect(left, y, traitColumnWidth, labelHeight), ASMKeys.Trait.Translate());
        left += traitColumnWidth;
        Widgets.Label(new Rect(left, y, UIConstants.ButtonMinWidth, labelHeight), ASMKeys.Keep.Translate());
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

    protected override float DrawRow(Rect row, int index)
    {
        var rule = RuleSet.Get(index);

        // TODO: filter nulls in settings
        if (rule.trait is null)
        {
            return 0;
        }

        var anchor = Text.Anchor;
        var left = row.x;
        var top = row.y;

        left += KindSlaughterSettingsTabListHelper.DrawGrip(left, top, row.height);

        var right = row.xMax - (UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.IconSize + UIConstants.GapX +
            UIConstants.IconSize);
        var traitButtonWidth = right - left;
        top = row.y + (row.height - UIConstants.ButtonHeight) / 2f;

        left += TraitRuleListButton.Draw(new Rect(left, top, traitButtonWidth, UIConstants.ButtonHeight), rule.trait, rule.Label,
            () => Find.WindowStack.Add(new Dialog_TraitPicker(picked => ReplaceTraits(index, picked))));

        Text.Anchor = TextAnchor.MiddleCenter;

        var keepCountValue = Widgets.TextField(new Rect(left, top, UIConstants.ButtonMinWidth, UIConstants.ButtonHeight), rule.keepCount.ToString());

        if (int.TryParse(keepCountValue, out int n) && n >= 0 && n != rule.keepCount)
        {
            rule.keepCount = n;
            SettingsChanges.Raise();
        }

        Text.Anchor = TextAnchor.UpperLeft;

        left += UIConstants.ButtonMinWidth + UIConstants.GapX;

        // TODO: button width
        left += AgeDropdown.Draw(left, top, UIConstants.ButtonMinWidth, rule.ageScope, val => { rule.ageScope = val; SettingsChanges.Raise(); });
        left += UIConstants.GapX;
        // TODO: button width
        left += GenderDropdown.Draw(left, top, UIConstants.ButtonMinWidth, rule.genderScope, val => { rule.genderScope = val; SettingsChanges.Raise(); });
        left += UIConstants.GapX;
        // TODO: button width
        left += TraitInheritabilityDropdown.Draw(left, top, UIConstants.ButtonMinWidth, rule.inheritMode, value => { rule.inheritMode = value; SettingsChanges.Raise(); });
        left += UIConstants.GapX;

        top = row.y + (row.height - UIConstants.IconSize) / 2f;
        var copyButtonRect = new Rect(left, top, UIConstants.IconSize, UIConstants.IconSize);

        if (KindSlaughterSettingsTabListHelper.CopyButton(copyButtonRect, ASMKeys.Copy))
        {
            RuleSet.CopyAt(index);
        }

        left += UIConstants.IconSize + UIConstants.GapX;
        var removeButtonRect = new Rect(left, top, UIConstants.IconSize, UIConstants.IconSize);

        if (KindSlaughterSettingsTabListHelper.RemoveButton(removeButtonRect, ASMKeys.RemoveTrait))
        {
            RuleSet.RemoveAt(index);
        }

        Text.Anchor = anchor;

        return row.height;
    }
}
