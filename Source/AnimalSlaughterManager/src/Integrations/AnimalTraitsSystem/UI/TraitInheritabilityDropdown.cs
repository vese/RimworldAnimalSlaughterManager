using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

public static class TraitInheritabilityDropdown
{
    public static float Draw(float x, float y, float width, TraitInheritability currentValue, Action<TraitInheritability> setValue)
    {
        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.UpperLeft;

        var height = UIConstants.ButtonHeight;
        if (Widgets.ButtonText(new Rect(x, y, width, height), currentValue.Translate()))
        {
            var options = new List<FloatMenuOption>();

            foreach (var item in (TraitInheritability[])Enum.GetValues(typeof(TraitInheritability)))
            {
                options.Add(new FloatMenuOption(item.Translate(), () => setValue(item)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        Text.Anchor = anchor;

        return height;
    }
}
