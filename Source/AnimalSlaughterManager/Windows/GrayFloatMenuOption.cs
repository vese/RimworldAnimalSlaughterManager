using System;
using UnityEngine;
using Verse;

namespace ASM;

// FloatMenuOption with a custom GUI.color tint (for gray unavailable trainables).
public class GrayFloatMenuOption : FloatMenuOption
{
    private readonly Color tint;

    public GrayFloatMenuOption(string label, Action action, Texture2D? icon, Color iconColor, Color tint) : base(label, action, icon, iconColor)
    {
        this.tint = tint;
    }

    public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
    {
        var prev = GUI.color;
        GUI.color = tint;
        var click = base.DoGUI(rect, colonistOrdering, floatMenu);
        GUI.color = prev;
        return click;
    }
}
