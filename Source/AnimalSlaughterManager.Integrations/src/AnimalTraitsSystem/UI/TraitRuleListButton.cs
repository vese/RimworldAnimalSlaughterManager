using System;
using UnityEngine;
using Verse;

namespace ASM;

public static class TraitRuleListButton
{
    public static float Draw(Rect rect, HediffDef trait, string label, Action onClick)
    {
        Widgets.DrawHighlightIfMouseover(rect);

        if (Widgets.ButtonInvisible(rect))
        {
            onClick();
        }

        GUI.color = AnimalTraitsAccess.TraitColor(trait);
        Text.Anchor = TextAnchor.MiddleLeft;
        bool wrap = Text.WordWrap;
        Text.WordWrap = false;

        var labelRect = new Rect(
            rect.x + KindSlaughterSettingsTabListHelper.ListRowPaddingX,
            rect.y,
            rect.width - KindSlaughterSettingsTabListHelper.ListRowPaddingX,
            rect.height);
        Widgets.Label(labelRect, label);

        Text.WordWrap = wrap;
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        TooltipHandler.TipRegion(rect, AnimalTraitsAccess.TraitTip(trait));

        return rect.width;
    }
}
