using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>Reusable rows list: scrolling, zebra striping, drag-reordering. The consumer
/// renders row content over the full row rect via drawRow (grip first — DrawGrip).</summary>
public static class ReorderableList
{
    public const float RowGapY = 2f;
    public const float RowPaddingX = 4f;

    private static readonly Color DividerColor = new(1f, 1f, 1f, 0.5f);

    public static float RowHeight => Text.LineHeight + UIConstants.TextPaddingY;

    public static void Draw(float x, float y, float width, float height, int count,
        ReorderableListState state, Action<int, int> reorder, Action<Rect, int> drawRow)
    {
        var top = y;
        var rowHeight = RowHeight;
        var contentHeight = count * (rowHeight + RowGapY);
        var outRect = new Rect(x, top, width, height);
        var view = new Rect(x, top, width - UIConstants.ScrollbarWidth, Mathf.Max(contentHeight, outRect.height));

        Widgets.BeginScrollView(outRect, ref state.scroll, view);

        if (Event.current.type == EventType.Repaint)
        {
            state.group = ReorderableWidget.NewGroup(reorder, ReorderableDirection.Vertical, outRect);
        }

        for (int i = 0; i < count; i++)
        {
            var row = new Rect(view.x, top, view.width, rowHeight);

            if (i % 2 == 1)
            {
                Widgets.DrawAltRect(row);
            }

            ReorderableWidget.Reorderable(state.group, new Rect(row.x, row.y, UIConstants.IconSize, row.height));

            drawRow(row, i);

            top += row.height + RowGapY;
        }

        Widgets.EndScrollView();
    }

    public static float DrawGrip(float x, float y, float height)
    {
        var anchor = Text.Anchor;
        var color = GUI.color;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = DividerColor;

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
