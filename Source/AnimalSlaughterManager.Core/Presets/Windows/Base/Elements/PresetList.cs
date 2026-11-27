using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

internal static class PresetList
{
    private const float RowPadX = 8f; // preset row left/right padding

    // The zebra list of preset rows inside a scroll view; the empty state labels the filter.
    public static float Draw(float x, float y, float width, float height,
        List<PresetEntry> entries, string filterBuffer, EntryRowController controller,
        ref Vector2 scroll, ref float listHeight)
    {
        var outRect = new Rect(x, y, width, height);
        var view = new Rect(x, y, width - UIConstants.ScrollbarWidth, Mathf.Max(listHeight, outRect.height));
        Widgets.BeginScrollView(outRect, ref scroll, view);

        float cy = view.y;

        if (entries.Count == 0)
        {
            var color = GUI.color;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(view.x, cy + UIConstants.TextPaddingY, view.width, Text.LineHeight),
                filterBuffer.Trim().NullOrEmpty() ? ASMKeys.NoPresets.Translate() : ASMKeys.NoPresetsFiltered.Translate());
            GUI.color = color;
            cy += Text.LineHeight + 2f * UIConstants.TextPaddingY;
        }

        int index = 0;

        foreach (var entry in entries)
        {
            var row = new Rect(view.x + RowPadX, cy, view.width - 2f * RowPadX, BasePresetBrowserDialog.RowHeight);
            EntryRow.Draw(row, entry, index, controller);
            cy += BasePresetBrowserDialog.RowHeight + UIConstants.GapX;
            index++;
        }

        listHeight = Mathf.Max(cy - view.y, outRect.height);
        Widgets.EndScrollView();
        return outRect.yMax;
    }
}
