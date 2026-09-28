using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

public static class AgeDropdown
{
    public static float Draw(float x, float y, float width, AgeScope currentValue, Action<AgeScope> setValue)
    {
        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.UpperLeft;

        var height = UIConstants.ButtonHeight;
        if (Widgets.ButtonText(new Rect(x, y, width, height), currentValue.Translate()))
        {
            var options = new List<FloatMenuOption>();

            foreach (var item in (AgeScope[])Enum.GetValues(typeof(AgeScope)))
            {
                options.Add(new FloatMenuOption(item.Translate(), () => setValue(item)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        Text.Anchor = anchor;

        return height;
    }
}
