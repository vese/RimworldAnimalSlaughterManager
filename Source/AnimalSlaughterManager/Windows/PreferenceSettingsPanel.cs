using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

public class PreferenceSettingsPanel(PreferenceSettings settings, Action<bool, bool, SlaughterPreference> setPreference, string helpTextKey)
{
    private const string selectedButtonTextPrefix = "[✓] ";
    private const string notSelectedButtonTextPrefix = "[   ] ";
    private static float ButtonPrefixWidth => MathF.Max(Text.CalcSize(selectedButtonTextPrefix).x, Text.CalcSize(notSelectedButtonTextPrefix).x);

    private static readonly List<(string TextKey, SlaughterPreference Value)> PreferenceChoices =
    [
        (ASMKeys.OldestFirst, SlaughterPreference.OldestFirst),
        (ASMKeys.YoungestFirst, SlaughterPreference.YoungestFirst)
    ];

    public float Draw(float x, float y, float width)
    {
        var top = y;

        top = DrawPreferenceRows(x, top);

        top += UIConstants.GapY;

        top = DrawHelp(x, top, width, helpTextKey);

        return top;
    }

    private float DrawPreferenceRows(float x, float y)
    {
        var top = y;
        var labels = KindPrioritySettings.ruleSetsNames.Values.Select(x => x.Translate()).ToList();
        var labelWidth = labels.Max(x => Text.CalcSize(x).x);
        var buttons = PreferenceChoices.Select(x => (Text: x.TextKey.Translate(), x.Value)).ToList();
        var buttonsWidth = buttons.Max(x => Text.CalcSize(x.Text).x) + ButtonPrefixWidth + UIConstants.ButtonPaddingX;

        for (var i = 0; i < KindPrioritySettings.keys.Count; i++)
        {
            if (i > 0)
            {
                top += UIConstants.GapY;
            }

            var key = KindPrioritySettings.keys[i];
            var label = labels[i];
            var value = settings.Get(key.Male, key.Adult);

            void setValue(SlaughterPreference value) => setPreference(key.Male, key.Adult, value);

            top += DrawPreferenceRow(x, top, labelWidth, label, buttonsWidth, buttons, value, setValue);
        }

        return top;
    }

    private float DrawPreferenceRow(
        float x,
        float y,
        float labelWidth,
        TaggedString label,
        float buttonsWidth,
        List<(TaggedString Text, SlaughterPreference Value)> buttons,
        SlaughterPreference currentValue,
        Action<SlaughterPreference> setValue)
    {
        var color = GUI.color;
        var font = Text.Font;
        var anchor = Text.Anchor;
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        var height = Text.LineHeight + UIConstants.ButtonPaddingY;
        var left = x;

        Widgets.Label(new Rect(left, y, labelWidth, height), label);

        left += labelWidth + UIConstants.GapX;

        foreach (var button in buttons)
        {
            var buttonRect = new Rect(left, y, buttonsWidth, height);
            var buttonText = GetButtonText(button.Text, currentValue == button.Value);

            if (Widgets.ButtonText(buttonRect, buttonText))
            {
                setValue(button.Value);
            }

            left += buttonsWidth + UIConstants.GapX;
        }

        GUI.color = color;
        Text.Font = font;
        Text.Anchor = anchor;

        return height;
    }

    private static string GetButtonText(string text, bool selected) => (selected ? selectedButtonTextPrefix : notSelectedButtonTextPrefix) + text;

    private float DrawHelp(float x, float y, float width, string helpTextKey)
    {
        var color = GUI.color;
        var font = Text.Font;
        GUI.color = Color.gray;
        Text.Font = GameFont.Tiny;

        var helpText = helpTextKey.Translate();
        var helpHeight = Text.CalcHeight(helpText, width);

        Widgets.Label(new Rect(x, y, width, helpHeight), helpText);

        GUI.color = color;
        Text.Font = font;

        return y + helpHeight;
    }
}
