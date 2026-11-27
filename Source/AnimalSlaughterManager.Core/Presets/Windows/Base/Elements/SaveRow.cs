using System;
using System.Collections.Generic;
using System.Text;

using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

internal static class SaveRow
{
    // Vanilla's own filename limit (GenText.IsValidFilename).
    private const int MaxNameLength = 40;

    private static readonly HashSet<char> invalidNameChars = [.. GenText.GetInvalidFilenameCharacters()];

    // Bottom row: name field + Save. Disabled when there is nothing to save or no name.
    public static float Draw(float x, float y, float width, bool hasData, Action<string> save, ref string nameBuffer)
    {
        float saveWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(ASMKeys.SavePreset.Translate()).x + UIConstants.ButtonPaddingX);
        bool canSave = hasData && !nameBuffer.Trim().NullOrEmpty();
        float fieldY = y + (BasePresetBrowserDialog.RowHeight - UIConstants.ButtonHeight) / 2f;

        GUI.enabled = hasData;
        nameBuffer = FilterFileName(Widgets.TextField(new Rect(x, fieldY, width - saveWidth - UIConstants.GapX, UIConstants.ButtonHeight), nameBuffer, MaxNameLength));

        if (Widgets.ButtonText(new Rect(x + width - saveWidth, fieldY, saveWidth, UIConstants.ButtonHeight), ASMKeys.SavePreset.Translate(), active: canSave) && canSave)
        {
            string name = nameBuffer.Trim();
            save(name);
            nameBuffer = string.Empty;
            Messages.Message(ASMKeys.PresetSaved.Translate(name), MessageTypeDefOf.TaskCompletion, false);
        }

        GUI.enabled = true;
        return y + BasePresetBrowserDialog.RowHeight;
    }

    // The name becomes a file name, so the invalid characters never enter the field at all —
    // the repository writes the name as-is.
    private static string FilterFileName(string name)
    {
        var sb = new StringBuilder(name.Length);
        var changed = false;

        foreach (var c in name)
        {
            if (invalidNameChars.Contains(c))
            {
                changed = true;
            }
            else
            {
                sb.Append(c);
            }
        }

        return changed ? sb.ToString() : name;
    }
}
