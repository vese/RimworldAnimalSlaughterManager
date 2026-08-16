using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

public class PreferenceSettingsPanel(float gapX, float gapY, float buttonHeight, float buttonPaddingX)
{
    private static readonly Dictionary<string, SlaughterPreference> PreferenceChoices = new()
    {
        { ASMKeys.OldestFirst, SlaughterPreference.OldestFirst },
        { ASMKeys.YoungestFirst, SlaughterPreference.YoungestFirst }
    };

    public float Draw(float x, float y, float width,
        KindSettings settings,
        ASM_MapComp comp,
        Action<KindSettings, ASM_MapComp, bool, bool, SlaughterPreference> setPreference,
        string helpTextKey)
    {
        var preferences = KindPrioritySettings.ruleSetsNames.Select(x => (x.Key, Text: x.Value.Translate()));
        var labelWidth = preferences.Max(x => Text.CalcSize(x.Text).x);
        var top = y;

        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        foreach (var preference in preferences)
        {
            DrawPreferenceRow(x, top, labelWidth, preference.Text,
                settings.preferenceSettings.GetPref(preference.Key.Male, preference.Key.Adult),
                (value) => setPreference(settings, comp, preference.Key.Male, preference.Key.Adult, value));

            top += buttonHeight + gapY;
        }

        var helpText = helpTextKey.Translate();
        var helpHeight = Text.CalcHeight(helpText, width);

        GUI.color = Color.gray;
        Text.Font = GameFont.Tiny;

        Widgets.Label(new Rect(x, top, width, helpHeight), helpText);

        top += helpHeight;

        return top;
    }

    private void DrawPreferenceRow(float x, float y, float textWidth, TaggedString label, SlaughterPreference setting, Action<SlaughterPreference> setPreference)
    {
        Widgets.Label(new Rect(x, y, textWidth, buttonHeight), label);

        var choices = PreferenceChoices.Select(x => (Text: x.Key.Translate(), Setting: x.Value));
        var buttonsWidth = choices.Max(x => Text.CalcSize(x.Text).x) + buttonPaddingX;
        var left = x + textWidth + gapX;

        foreach (var choice in choices)
        {
            if (ChoiceButton(new Rect(x + textWidth + gapX, y, buttonsWidth, buttonHeight), choice.Text, setting == choice.Setting))
            {
                setPreference(choice.Setting);
            }

            left += buttonsWidth + gapX;
        }
    }

    private static bool ChoiceButton(Rect r, string label, bool selected) =>
        Widgets.ButtonText(r, (selected ? "[✓] " : "[  ] ") + label);
}
