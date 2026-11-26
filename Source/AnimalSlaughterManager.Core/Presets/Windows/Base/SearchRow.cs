using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

internal static class SearchRow
{
    // Top row: magnifier + filter field on the left, clear ✕ after it; scope-type toggles
    // right-aligned to the window edge.
    public static float Draw(float x, float y, float width, List<PresetScope> scopes,
        ref Dictionary<PresetScope, bool> scopesState, ref string filterBuffer)
    {
        var left = x;
        var right = left + width;
        var top = y;
        var height = UIConstants.ButtonHeight;
        var iconTop = top + (height - UIConstants.IconSize) / 2f;

        var searchIconRect = new Rect(left, iconTop, UIConstants.IconSize, UIConstants.IconSize);

        GUI.DrawTexture(searchIconRect, TexButton.Search);

        var hasScopeToggles = scopes.Count > 1;
        var clearButtonRight = right;

        if (hasScopeToggles)
        {
            clearButtonRight = DrawScopeToggles(right, y, height, scopes, ref scopesState);
        }

        var clearButtonRect = new Rect(clearButtonRight - UIConstants.IconSize, iconTop, UIConstants.IconSize, UIConstants.IconSize);

        TooltipHandler.TipRegion(clearButtonRect, ASMKeys.Clear.Translate());

        if (Widgets.ButtonImage(clearButtonRect, TexButton.CloseXSmall))
        {
            filterBuffer = string.Empty;
        }

        var filterInputLeft = searchIconRect.xMax + UIConstants.GapX;
        var filterInputRect = new Rect(filterInputLeft, top, clearButtonRect.x - UIConstants.GapX - filterInputLeft, height);

        filterBuffer = Widgets.TextField(filterInputRect, filterBuffer);

        return top + height;
    }

    // The toggles sit flush right, laid out in the scopes' numeric order.
    private static float DrawScopeToggles(float right, float y, float height, List<PresetScope> scopes, ref Dictionary<PresetScope, bool> scopesState)
    {
        foreach (var scope in scopes)
        {
            right -= DrawScopeToggle(right, y, height, scope, ref scopesState);
            right -= UIConstants.GapX;
        }

        return right;
    }

    private static float DrawScopeToggle(float right, float y, float height, PresetScope scope, ref Dictionary<PresetScope, bool> scopesState)
    {
        var on = !scopesState.TryGetValue(scope, out var shown) || shown;
        var scopeMark = GetScopeMark(scope);

        var width = UIConstants.IconSize + UIConstants.GapX + Text.CalcSize(scopeMark).x;
        var left = right - width;
        var top = y;

        var buttonRect = new Rect(left, top, width, height);

        Widgets.DrawHighlightIfMouseover(buttonRect);

        var iconRect = new Rect(left, top + (height - UIConstants.IconSize) / 2f, UIConstants.IconSize, UIConstants.IconSize);

        var color = GUI.color;

        if (on)
        {
            GUI.color = Color.green;
            Widgets.CheckboxDraw(iconRect.x, iconRect.y, true, false, UIConstants.IconSize);
        }
        else
        {
            GUI.color = Color.red;
            GUI.DrawTexture(iconRect, TexButton.CloseXSmall);
        }

        GUI.color = color;

        var textLeftOffset = UIConstants.IconSize + UIConstants.GapX;

        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleLeft;

        var labelRect = new Rect(left + textLeftOffset, top, width - textLeftOffset, height);

        Widgets.Label(labelRect, scopeMark);

        Text.Anchor = anchor;

        if (Widgets.ButtonInvisible(buttonRect))
        {
            scopesState[scope] = !on;
        }

        return left;
    }

    private static TaggedString GetScopeMark(PresetScope scope) => scope switch
    {
        PresetScope.All => ASMKeys.PresetAllMark.Translate(),
        PresetScope.Kind => ASMKeys.PresetKindMark.Translate(),
        PresetScope.List => ASMKeys.PresetListMark.Translate(),
        _ => throw new NotImplementedException(),
    };
}
