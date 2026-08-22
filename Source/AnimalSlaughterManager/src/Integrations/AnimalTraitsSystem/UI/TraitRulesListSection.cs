using UnityEngine;
using Verse;

namespace ASM;

public class TraitRulesListSection(IEditableTraitsRuleSet<TraitRule> rules) : BaseTraitRulesListSection<TraitRule>(rules)
{
    protected override string TitleKey { get; } = ASMKeys.KeepTraits;
    protected override string HelpKey { get; } = ASMKeys.KeepTraitsHelp;

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

        var left = x;
        //TODO: width
        var traitButtonWidth = width -
            UIConstants.IconSize -
            UIConstants.ButtonMinWidth - UIConstants.GapX -
            UIConstants.ButtonMinWidth - UIConstants.GapX -
            UIConstants.ButtonMinWidth - UIConstants.GapX -
            UIConstants.IconSize - UIConstants.GapX -
            UIConstants.IconSize;

        Widgets.Label(new Rect(left, y, traitButtonWidth, Text.SmallFontHeight), ASMKeys.Trait.Translate());
        Widgets.Label(new Rect(left, y, UIConstants.ButtonMinWidth, Text.SmallFontHeight), ASMKeys.AgeScope.Translate());
        Widgets.Label(new Rect(left, y, UIConstants.ButtonMinWidth, Text.SmallFontHeight), ASMKeys.GenderScope.Translate());
        Widgets.Label(new Rect(left, y, UIConstants.ButtonMinWidth, Text.SmallFontHeight), ASMKeys.InheritMode.Translate());

        Text.WordWrap = wrap;
        Text.Anchor = anchor;
        Text.Font = font;
        GUI.color = color;

        return Text.SmallFontHeight;
    }

    protected override float DrawRow(Rect row, int index, ASM_MapComp comp)
    {
        var rule = RuleSet.Get(index);

        // TODO: filter nulls in settings
        if (rule.trait is null)
        {
            return 0;
        }

        var left = row.x;
        var top = row.y;

        left += KindSlaughterSettingsTabListHelper.DrawGrip(left, top, row.height);

        var right = row.xMax -= UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.ButtonMinWidth + UIConstants.GapX +
            UIConstants.IconSize + UIConstants.GapX +
            UIConstants.IconSize;
        var traitButtonWidth = right - left;
        top = row.y + (row.height - UIConstants.ButtonHeight) / 2f;

        left += TraitRuleListButton.Draw(new Rect(left, top, traitButtonWidth, UIConstants.ButtonHeight), rule.trait, rule.Label,
            () => Find.WindowStack.Add(new Dialog_TraitPicker(picked => ReplaceTraits(index, picked, comp))));

        Text.Anchor = TextAnchor.UpperLeft;

        // TODO: button width
        left += AgeDropdown.Draw(left, top, UIConstants.ButtonMinWidth, rule.ageScope, val => { rule.ageScope = val; comp.MarkDirty(); });
        left += UIConstants.GapX;
        // TODO: button width
        left += GenderDropdown.Draw(left, top, UIConstants.ButtonMinWidth, rule.genderScope, val => { rule.genderScope = val; comp.MarkDirty(); });
        left += UIConstants.GapX;
        // TODO: button width
        left += TraitInheritabilityDropdown.Draw(left, top, UIConstants.ButtonMinWidth, rule.inheritMode, value => { rule.inheritMode = value; comp.MarkDirty(); });
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
            comp.MarkDirty();
        }

        return row.height;
    }
}
