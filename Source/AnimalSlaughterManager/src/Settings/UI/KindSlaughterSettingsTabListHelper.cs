using RimWorld;
using System.Collections;
using UnityEngine;
using Verse;

namespace ASM;

public static class KindSlaughterSettingsTabListHelper
{
    public const float ListGapY = 2f;
    public const float ListRowPaddingX = 4f;
    private static readonly Color ListDividerColor = new(1f, 1f, 1f, 0.5f);

    public static void ReorderList(IList list, int from, int to)
    {
        if (from < 0 || from >= list.Count || to < 0 || to > list.Count || from == to)
        {
            return;
        }

        var item = list[from];

        list.RemoveAt(from);

        if (from < to)
        {
            list.Insert(to - 1, item);
        }
        else
        {
            list.Insert(to, item);
        }
    }

    public static float DrawGrip(float x, float y, float height)
    {
        var anchor = Text.Anchor;
        var color = GUI.color;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = ListDividerColor;

        Widgets.Label(new Rect(x, y, UIConstants.IconSize, height), "≡");

        Text.Anchor = anchor;
        GUI.color = color;

        return UIConstants.IconSize;
    }

    // Red X remove icon, like the clear buttons in the manager table.
    public static bool RemoveButton(Rect buttonRect, string tooltipKey)
    {
        TooltipHandler.TipRegion(buttonRect, tooltipKey.Translate());

        GUI.color = Color.red;

        var click = Widgets.ButtonImage(buttonRect, TexButton.CloseXSmall);

        return click;
    }
    public static bool CopyButton(Rect buttonRect, string tooltipKey)
    {
        TooltipHandler.TipRegion(buttonRect, tooltipKey.Translate());

        GUI.color = Color.white;

        var click = Widgets.ButtonImage(buttonRect, TexButton.Copy);

        return click;
    }
}
