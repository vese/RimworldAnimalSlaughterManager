using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>Generic dropdown widget: a button showing the current label; a click opens the
/// ready-made options in a float menu. Owns the rendering (button, FloatMenu).</summary>
public static class Dropdown
{
    /// <summary>Draws the button; returns the horizontal space it occupies.</summary>
    public static float Draw(float x, float y, float width, TaggedString label, List<FloatMenuOption> options)
    {
        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.UpperLeft;

        var height = UIConstants.ButtonHeight;
        if (Widgets.ButtonText(new Rect(x, y, width, height), label))
        {
            Find.WindowStack.Add(new FloatMenu(options));
        }

        Text.Anchor = anchor;

        return width;
    }

    /// <summary>Menu options for values: labelOf renders each value, setValue applies the choice.</summary>
    public static List<FloatMenuOption> Options<T>(IEnumerable<T> values, Func<T, TaggedString> labelOf, Action<T> setValue)
    {
        return [.. values.Select(value => new FloatMenuOption(labelOf(value), () => setValue(value)))];
    }

    /// <summary>Dropdown over an enum's values: labelOf renders each value, setValue applies it.</summary>
    public static float DrawEnum<T>(float x, float y, float width, T current, Func<T, TaggedString> labelOf, Action<T> setValue) where T : struct, Enum
    {
        return Draw(x, y, width, labelOf(current), Options(Enum.GetValues(typeof(T)).Cast<T>(), labelOf, setValue));
    }
}
