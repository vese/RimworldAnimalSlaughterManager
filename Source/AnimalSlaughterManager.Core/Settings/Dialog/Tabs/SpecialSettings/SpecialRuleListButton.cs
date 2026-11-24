using System;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>The rule's target button: opens the picker, painted through the options source
/// (label color and tooltip are the integration's).</summary>
public static class SpecialRuleListButton
{
    public static float Draw(Rect rect, SpecialRule rule, ISpecialRuleOptionsSource? options, Action onClick)
    {
        Widgets.DrawHighlightIfMouseover(rect);

        if (Widgets.ButtonInvisible(rect))
        {
            onClick();
        }

        GUI.color = rule.target != null ? options?.ColorOf(rule.target) ?? Color.white : Color.white;
        Text.Anchor = TextAnchor.MiddleLeft;
        bool wrap = Text.WordWrap;
        Text.WordWrap = false;

        var labelRect = new Rect(
            rect.x + ReorderableList.RowPaddingX,
            rect.y,
            rect.width - ReorderableList.RowPaddingX,
            rect.height);
        Widgets.Label(labelRect, rule.Label);

        Text.WordWrap = wrap;
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        if (rule.target != null)
        {
            TooltipHandler.TipRegion(rect, options?.TipOf(rule.target));
        }

        return rect.width;
    }
}
