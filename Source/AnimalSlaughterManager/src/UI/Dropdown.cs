using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>Generic dropdown widget: shows the controller's current label and opens a float
/// menu of its values. Owns the rendering components (FloatMenu/FloatMenuOption).</summary>
public static class Dropdown
{
    public static float Draw<T>(float x, float y, DropdownController<T> controller)
    {
        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.UpperLeft;

        var height = UIConstants.ButtonHeight;
        if (Widgets.ButtonText(new Rect(x, y, controller.Width, height), controller.LabelOf(controller.Current)))
        {
            var options = controller.Values
                .Select(value => new FloatMenuOption(controller.LabelOf(value), () => controller.SetValue(value)))
                .ToList();

            Find.WindowStack.Add(new FloatMenu(options));
        }

        Text.Anchor = anchor;

        return height;
    }

    /// <summary>Dropdown over an enum's values: labelOf renders each value, setValue applies it.</summary>
    public static float DrawEnum<T>(float x, float y, float width, T current, Func<T, TaggedString> labelOf, Action<T> setValue)
        where T : struct, Enum
    {
        return Draw(x, y, new DropdownController<T>
        {
            Width = width,
            Current = current,
            Values = Enum.GetValues(typeof(T)).Cast<T>(),
            LabelOf = labelOf,
            SetValue = setValue
        });
    }
}
