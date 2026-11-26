using System;

using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

internal static class SaveRow
{
    // Bottom row: name field + Save. Disabled when there is nothing to save or no name.
    public static float Draw(float x, float y, float width, bool hasData, Action<string> save, ref string nameBuffer)
    {
        float saveWidth = BasePresetBrowserDialog.GetButtonWidth(ASMKeys.SavePreset.Translate());
        bool canSave = hasData && !nameBuffer.Trim().NullOrEmpty();
        float fieldY = y + (BasePresetBrowserDialog.RowHeight - UIConstants.ButtonHeight) / 2f;

        GUI.enabled = hasData;
        nameBuffer = Widgets.TextField(new Rect(x, fieldY, width - saveWidth - UIConstants.GapX, UIConstants.ButtonHeight), nameBuffer);

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
}
