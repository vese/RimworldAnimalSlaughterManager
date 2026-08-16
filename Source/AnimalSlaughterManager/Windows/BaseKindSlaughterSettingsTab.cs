using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

public abstract class BaseKindSlaughterSettingsTab : IKindSlaughterSettingsDialogTab
{
    protected const float ScrollbarWidth = 16f;
    protected const float GapX = 6f;
    public const float GapY = 8f;
    public static float HeaderHeight => Text.LineHeightOf(GameFont.Medium);
    protected const float HeaderIconSize = 28f;
    protected const float HeaderMarginRight = 12f; // margin from the window close-X in the top-right corner
    protected const float LabelMarginLeft = HeaderIconSize + GapX;
    protected static float MediumTextHeight => Text.LineHeightOf(GameFont.Medium);
    protected const float ButtonHeight = 26f;
    protected const float ButtonMinWidth = 120f;
    protected const float ButtonPaddingX = 18f;
    protected static readonly float ButtonMarginTop = (HeaderHeight - ButtonHeight) / 2; //centered vertically
    public const float TabBarHeight = 32f;
    protected const float DividerGap = 2f;
    protected static readonly Color DividerColor = new(1f, 1f, 1f, 0.25f);
    protected const float IconSize = 22f;

    protected Vector2 topScroll;

    protected virtual List<(string Text, Action<ASM_MapComp, ThingDef, KindSettings> Action)> HeaderButtons { get; } = [];
    protected virtual float ContentMinHeight { get; } = 0f;
    protected virtual float WindowMinHeight => HeaderHeight + GapY + TabBarHeight + GapY + ContentMinHeight;

    public abstract TaggedString Name { get; }

    public void DoWindowContents(Rect inRect, ASM_MapComp comp, ThingDef animalDef, KindSettings settings, Action<float, float, float, float> drawTabBar)
    {
        if (inRect.height >= WindowMinHeight)
        {
            DrawWindow(inRect.x, inRect.y, inRect.width, inRect.height, comp, animalDef, settings, drawTabBar);
        }
        else
        {
            var width = inRect.width - ScrollbarWidth;
            Rect view = new Rect(inRect.x, inRect.y, width, WindowMinHeight);
            Widgets.BeginScrollView(inRect, ref topScroll, view);
            DrawWindow(view.x, view.y, width, inRect.height, comp, animalDef, settings, drawTabBar);
            Widgets.EndScrollView();
        }
    }

    private void DrawWindow(float x, float y, float width, float height, ASM_MapComp comp, ThingDef animalDef, KindSettings settings, Action<float, float, float, float> drawTabBar)
    {
        var top = y;
        DrawHeader(x, top, width, comp, animalDef, settings);
        top += HeaderHeight + GapY;

        // Tab strip. TabDrawer draws the tab buttons in the 32px band ABOVE the base rect (they
        // hang from the rect's top edge upward), so reserve that band here and put the base rect
        // at its bottom edge — otherwise the tabs render upward into the kind-name header.
        top += TabBarHeight;
        drawTabBar(x, top, width, TabBarHeight);
        top += +GapY;

        var contentHeight = height - (HeaderHeight + GapY + TabBarHeight + GapY);
        DrawTabContent(x, top, width, contentHeight, settings, comp, animalDef);
    }

    // Header: animal icon + name on the left; the per-kind preset button and the two reset
    // buttons sit to the right of the name. On the General tab they are hidden.
    protected void DrawHeader(float x, float y, float width, ASM_MapComp comp, ThingDef animalDef, KindSettings settings)
    {
        Widgets.ThingIcon(new Rect(x, y, HeaderIconSize, HeaderIconSize), animalDef);
        
        var right = x + width - HeaderMarginRight;
        var buttons = new List<(TaggedString Text, Rect Rect, Action<ASM_MapComp, ThingDef, KindSettings> Action)>(HeaderButtons.Count);

        foreach (var button in HeaderButtons)
        {
            var buttonText = button.Text.Translate();
            var buttonWidth = Mathf.Max(ButtonMinWidth, Text.CalcSize(buttonText).x + ButtonPaddingX);
            var buttonRect = new Rect(right - buttonWidth, y + ButtonMarginTop, buttonWidth, ButtonHeight);
            buttons.Add((buttonText, buttonRect, button.Action));
            right -= buttonWidth + GapX;
        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;

        Widgets.Label(new Rect(x + LabelMarginLeft, y, width, HeaderHeight), animalDef.LabelCap);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        foreach (var button in buttons)
        {
            if (Widgets.ButtonText(button.Rect, button.Text))
            {
                button.Action(comp, animalDef, settings);
            }
        }
    }

    protected abstract void DrawTabContent(float x, float y, float w, float contentHeight, KindSettings settings, ASM_MapComp comp, ThingDef animalDef);

    protected static void DrawDivider(float x, float y, float width)
    {
        GUI.color = DividerColor;
        Widgets.DrawLineHorizontal(x, y, width);
    }

    protected static float DrawDoubleDivider(float x, float y, float width)
    {
        var top = y;
        DrawDivider(x, top, width);
        top += DividerGap;
        DrawDivider(x, top, width);
        return top;
    }
}
