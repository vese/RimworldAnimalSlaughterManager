using UnityEngine;
using Verse;

namespace ASM;

public class TraitRulesListSection(IEditableTraitsRuleSet<TraitRule> rules) : BaseTraitRulesListSection<TraitRule>(rules)
{
    protected override string TitleKey { get; } = ASMKeys.ForceCullTraits;
    protected override string HelpKey { get; } = ASMKeys.ForceCullTraitsHelp;
    protected override string PresetsTitleKey { get; } = ASMKeys.PresetTitleForceCull;

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
        var rightSideWidth = UIConstants.ButtonMinWidth + UIConstants.GapX + // age
            UIConstants.ButtonMinWidth + UIConstants.GapX + // gender
            UIConstants.ButtonMinWidth + UIConstants.GapX + // inheritability
            UIConstants.IconSize + UIConstants.GapX + // copy
            UIConstants.IconSize; // remove
        var traitColumnWidth = width - gripOffset - rightSideWidth;
        var left = x + gripOffset;

        Widgets.Label(new Rect(left, y, traitColumnWidth, labelHeight), ASMKeys.Trait.Translate());
        left += traitColumnWidth;
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
            UIConstants.IconSize + UIConstants.GapX +
            UIConstants.IconSize);
        var traitButtonWidth = right - left;
        var buttonHeight = UIConstants.ButtonHeight;
        top = row.y + (row.height - buttonHeight) / 2f;

        left += TraitRuleListButton.Draw(new Rect(left, top, traitButtonWidth, buttonHeight), rule.trait, rule.Label,
            () => Find.WindowStack.Add(new Dialog_TraitPicker(picked => ReplaceTraits(index, picked))));

        Text.Anchor = TextAnchor.UpperLeft;

        // TODO: button width
        left += Dropdown.DrawEnum(left, top, UIConstants.ButtonMinWidth, rule.ageScope, val => val.Translate(), val => rule.SetAgeScope(val));
        left += UIConstants.GapX;
        // TODO: button width
        left += Dropdown.DrawEnum(left, top, UIConstants.ButtonMinWidth, rule.genderScope, val => val.Translate(), val => rule.SetGenderScope(val));
        left += UIConstants.GapX;
        // TODO: button width
        left += Dropdown.DrawEnum(left, top, UIConstants.ButtonMinWidth, rule.inheritMode, val => val.Translate(), val => rule.SetInheritability(val));
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
