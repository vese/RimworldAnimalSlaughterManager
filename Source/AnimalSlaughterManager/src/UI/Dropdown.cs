using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>Data for drawing a dropdown: the width, the current value's label and the options
/// (each carries its own setter). Rules and settings supply it; <see cref="Dropdown"/> draws it.</summary>
public sealed class DropdownControl
{
    public float Width = UIConstants.ButtonMinWidth;
    public TaggedString CurrentLabel;
    public IEnumerable<FloatMenuOption> Options = [];
}

/// <summary>Generic dropdown button: shows the current label and opens a float menu of options.</summary>
public static class Dropdown
{
    public static float Draw(float x, float y, DropdownControl control)
    {
        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.UpperLeft;

        var height = UIConstants.ButtonHeight;
        if (Widgets.ButtonText(new Rect(x, y, control.Width, height), control.CurrentLabel))
        {
            Find.WindowStack.Add(new FloatMenu(control.Options.ToList()));
        }

        Text.Anchor = anchor;

        return height;
    }

    /// <summary>Dropdown over an enum's values: labelOf renders each value, setValue applies it.</summary>
    public static float DrawEnum<T>(float x, float y, float width, T current, Func<T, TaggedString> labelOf, Action<T> setValue)
        where T : struct, Enum
    {
        var options = Enum.GetValues(typeof(T))
            .Cast<T>()
            .Select(value => new FloatMenuOption(labelOf(value), () => setValue(value)));

        return Draw(x, y, new DropdownControl { Width = width, CurrentLabel = labelOf(current), Options = options });
    }
}
