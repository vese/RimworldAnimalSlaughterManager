using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

internal static class EntryRow
{
    private const float DateColumnWidth = 86f;
    private const float DeleteColumnWidth = 26f;
    private const float MarkColumnMinWidth = 80f;
    private const float NameColumnMinWidth = 40f;

    public static void Draw(Rect row, PresetEntry entry, int index, EntryRowPolicy policy)
    {
        // Zebra rows, like the save/load list.
        if (index % 2 == 1)
        {
            Widgets.DrawAltRect(row);
        }

        var mark = policy.GetMark(entry);

        // Columns always reserved right-to-left. Overwrite (left of Load) only on rows the
        // policy allows when there is data to overwrite with.
        float loadWidth = BasePresetBrowserDialog.GetButtonWidth(ASMKeys.LoadPreset.Translate());
        float overwriteWidth = BasePresetBrowserDialog.GetButtonWidth(ASMKeys.OverwritePreset.Translate());
        float rowHeight = BasePresetBrowserDialog.RowHeight;
        float controlOffset = (rowHeight - UIConstants.ButtonHeight) / 2f;
        float labelOffset = (rowHeight - Text.LineHeight) / 2f;

        float right = row.xMax;
        var deleteBtn = new Rect(right - DeleteColumnWidth, row.y + controlOffset, DeleteColumnWidth, UIConstants.ButtonHeight);
        right -= DeleteColumnWidth + UIConstants.GapX;
        var loadBtn = new Rect(right - loadWidth, row.y + controlOffset, loadWidth, UIConstants.ButtonHeight);
        right -= loadWidth + UIConstants.GapX;
        var overwriteBtn = new Rect(right - overwriteWidth, row.y + controlOffset, overwriteWidth, UIConstants.ButtonHeight);
        right -= overwriteWidth + UIConstants.GapX;
        var dateLabel = new Rect(right - DateColumnWidth, row.y + labelOffset, DateColumnWidth, Text.LineHeight);
        right -= DateColumnWidth + UIConstants.GapX;
        var markLabel = new Rect(right - GetMarkColumnWidth(), row.y + labelOffset, GetMarkColumnWidth(), Text.LineHeight);
        right -= GetMarkColumnWidth() + UIConstants.GapX;
        var nameRect = new Rect(row.x, row.y, Mathf.Max(right - row.x, NameColumnMinWidth), rowHeight);

        // 1st column — name.
        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(nameRect, entry.name);
        Text.Anchor = anchor;

        // Mark column — the policy's mark (cross-scope/cross-bucket) or empty.
        if (mark != null)
        {
            var color = GUI.color;
            GUI.color = Color.cyan;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(markLabel, mark);
            Text.Anchor = anchor;
            GUI.color = color;
        }

        // Date — Tiny font so it fits on one line (like the save/load window).
        var font = Text.Font;
        Text.Font = GameFont.Tiny;
        var dateColor = GUI.color;
        GUI.color = Color.gray;
        Widgets.Label(dateLabel, entry.date.ToString("yyyy-MM-dd HH:mm"));
        GUI.color = dateColor;
        Text.Font = font;

        // Overwrite button (left of Load), with confirmation.
        if (policy.CanOverwrite(entry))
        {
            if (Widgets.ButtonText(overwriteBtn, ASMKeys.OverwritePreset.Translate()))
            {
                var captured = entry;
                Find.WindowStack.Add(new Dialog_MessageBox(ASMKeys.ConfirmOverwrite.Translate(captured.name),
                    ASMKeys.OverwritePreset.Translate(),
                    () => policy.Overwrite(captured),
                    "No".Translate()) { doCloseX = true });
            }
        }

        var tip = policy.GetTip(entry);

        if (tip != null)
        {
            TooltipHandler.TipRegion(row, tip);
        }

        if (Widgets.ButtonText(loadBtn, ASMKeys.LoadPreset.Translate()))
        {
            if (policy.Apply(entry))
            {
                Messages.Message(ASMKeys.PresetLoaded.Translate(entry.name), MessageTypeDefOf.TaskCompletion, false);
            }
            else
            {
                Messages.Message(ASMKeys.PresetNoSlice.Translate(policy.TargetLabel), MessageTypeDefOf.RejectInput, false);
            }
        }

        if (policy.CanDelete(entry))
        {
            TooltipHandler.TipRegion(deleteBtn, ASMKeys.DeletePreset.Translate());

            if (Widgets.ButtonImage(deleteBtn, TexButton.Delete))
            {
                var captured = entry;
                Find.WindowStack.Add(new Dialog_MessageBox(policy.GetConfirmDeleteText(captured),
                    ASMKeys.DeletePreset.Translate(), () => PresetRepository.Delete(captured),
                    "No".Translate()) { doCloseX = true });
            }
        }
    }

    // The mark column fits the longest scope mark, with a floor.
    private static float GetMarkColumnWidth() =>
        Mathf.Max(MarkColumnMinWidth, Text.CalcSize(ASMKeys.PresetAllMark.Translate()).x + UIConstants.GapX);
}
