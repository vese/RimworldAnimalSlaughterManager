using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// The shared preset-browser frame: a search row with scope-type toggles on top, a zebra list
/// of preset rows (load / overwrite / delete, date, a mark column), and a save row pinned to
/// the bottom — the layout of RimWorld's save/load list. A window declares the scopes it
/// offers with their entry sources; a single scope hides the toggle strip. Subclasses also
/// provide saving, applying and the row marks. Rows always reserve the same set of columns, so
/// they stay aligned when a button or mark is absent. Entries sort newest-first and filter by
/// name and by the scope toggles.
/// </summary>
public abstract class BasePresetBrowserDialog : Window
{
    private const float RowPadX = 8f;          // preset row left/right padding
    private const float DateColumnWidth = 86f;
    private const float DeleteColumnWidth = 26f;
    private const float MarkColumnMinWidth = 80f;
    private const float NameColumnMinWidth = 40f;

    // Row heights follow the font, like the settings windows' controls.
    private static readonly float RowHeight = UIConstants.ButtonHeight + 2f * UIConstants.TextPaddingY;

    // The enum's own values in numeric order (All — the broadest — first).
    private static readonly PresetScope[] ScopeValues = [.. ((PresetScope[])Enum.GetValues(typeof(PresetScope))).OrderByDescending(x => (int)x)];

    private Vector2 scroll;
    private float listHeight = 9999f;
    private string nameBuffer = string.Empty;
    private string filterBuffer = string.Empty;
    private Dictionary<PresetScope, bool> scopesState = [];

    /// <summary>The target the presets apply to (a kind's or bucket's label): the argument the
    /// subclass's title key translates with and the name the no-slice message quotes.</summary>
    protected TaggedString targetLabel;

    public override Vector2 InitialSize => new(820f, 600f);

    // TODO: сделать, чтобы при использовании не было получения Entries
    /// <summary>The scopes this browser offers with their entry sources (its own scope plus
    /// any broader ones it imports from). One scope — no toggle strip.</summary>
    protected abstract (PresetScope Scope, IEnumerable<PresetEntry> Entries)[] ScopeSources { get; }

    /// <summary>True when the target actually has data worth saving (enables the save row).</summary>
    protected abstract bool HasData { get; }

    protected BasePresetBrowserDialog()
    {
        doCloseX = true;
        draggable = true;
        resizeable = true;
    }

    /// <summary>Saves the target under the name. The base clears the name field and reports.</summary>
    protected abstract void Save(string name);

    /// <summary>Applies the preset; false = the preset had no usable slice for this target.</summary>
    protected abstract bool Apply(PresetEntry entry);

    /// <summary>Returns the row's mark-column content (cross-scope or cross-bucket mark); null = empty.</summary>
    protected virtual string? GetRowMark(PresetEntry entry) => null;

    /// <summary>Returns the row's tooltip; null = none.</summary>
    protected virtual string? GetRowTip(PresetEntry entry) => null;

    /// <summary>Whether the row carries the Overwrite button.</summary>
    protected virtual bool CanOverwrite(PresetEntry entry) => HasData;

    /// <summary>Whether the row carries the Delete button.</summary>
    protected virtual bool CanDelete(PresetEntry entry) => true;

    /// <summary>The delete-confirmation message for the entry.</summary>
    protected virtual string GetConfirmDeleteText(PresetEntry entry) => ASMKeys.ConfirmDelete.Translate(entry.name);

    // ---- Frame ----------------------------------------------------------------------------

    public override void DoWindowContents(Rect inRect)
    {
        var font = Text.Font;
        Text.Font = GameFont.Small;

        var top = inRect.y + UIConstants.GapX;
        var scopes = ScopeValues.Where(scope => ScopeSources.Any(s => s.Scope == scope)).ToList();

        top += SearchRow.Draw(inRect.x, top, inRect.width, scopes, ref scopesState, ref filterBuffer);

        // The preset list fills everything between the search row and the save row.
        var listTop = top + UIConstants.GapX;
        var listBottom = inRect.yMax - RowHeight - UIConstants.GapX;
        DrawPresetList(inRect.x, listTop, inRect.width, listBottom - listTop);

        DrawSaveRow(inRect.x, inRect.yMax - RowHeight, inRect.width);

        Text.Font = font;
    }

    // The zebra list of preset rows inside a scroll view; the empty state labels the filter.
    private void DrawPresetList(float x, float y, float width, float height)
    {
        var entries = Gather(ScopeSources);
        var outRect = new Rect(x, y, width, height);
        var view = new Rect(x, y, width - UIConstants.ScrollbarWidth, Mathf.Max(listHeight, outRect.height));
        Widgets.BeginScrollView(outRect, ref scroll, view);

        float cy = view.y;

        if (entries.Count == 0)
        {
            GUI.color = Color.gray;
            Widgets.Label(new Rect(view.x, cy + UIConstants.TextPaddingY, view.width, Text.LineHeight),
                filterBuffer.Trim().NullOrEmpty() ? ASMKeys.NoPresets.Translate() : ASMKeys.NoPresetsFiltered.Translate());
            GUI.color = Color.white;
            cy += Text.LineHeight + 2f * UIConstants.TextPaddingY;
        }

        int index = 0;

        foreach (var entry in entries)
        {
            var row = new Rect(view.x + RowPadX, cy, view.width - 2f * RowPadX, RowHeight);
            DrawEntryRow(row, entry, index);
            cy += RowHeight + UIConstants.GapX;
            index++;
        }

        listHeight = Mathf.Max(cy - view.y, outRect.height);
        Widgets.EndScrollView();
    }

    // Bottom row: name field + Save. Disabled when there is nothing to save or no name.
    private void DrawSaveRow(float x, float y, float width)
    {
        float saveWidth = GetButtonWidth(ASMKeys.SavePreset.Translate());
        bool canSave = HasData && !nameBuffer.Trim().NullOrEmpty();
        float fieldY = y + (RowHeight - UIConstants.ButtonHeight) / 2f;

        GUI.enabled = HasData;
        nameBuffer = Widgets.TextField(new Rect(x, fieldY, width - saveWidth - UIConstants.GapX, UIConstants.ButtonHeight), nameBuffer);

        if (Widgets.ButtonText(new Rect(x + width - saveWidth, fieldY, saveWidth, UIConstants.ButtonHeight), ASMKeys.SavePreset.Translate(), active: canSave) && canSave)
        {
            string name = nameBuffer.Trim();
            Save(name);
            nameBuffer = string.Empty;
            Messages.Message(ASMKeys.PresetSaved.Translate(name), MessageTypeDefOf.TaskCompletion, false);
        }

        GUI.enabled = true;
    }

    private void DrawEntryRow(Rect row, PresetEntry e, int index)
    {
        // Zebra rows, like the save/load list.
        if (index % 2 == 1)
        {
            Widgets.DrawAltRect(row);
        }

        var mark = GetRowMark(e);

        // Columns always reserved right-to-left. Overwrite (left of Load) only on rows the
        // policy allows when there is data to overwrite with.
        float loadWidth = GetButtonWidth(ASMKeys.LoadPreset.Translate());
        float overwriteWidth = GetButtonWidth(ASMKeys.OverwritePreset.Translate());

        float right = row.xMax;
        var deleteBtn = new Rect(right - DeleteColumnWidth, row.y + (RowHeight - UIConstants.ButtonHeight) / 2f, DeleteColumnWidth, UIConstants.ButtonHeight);
        right -= DeleteColumnWidth + UIConstants.GapX;
        var loadBtn = new Rect(right - loadWidth, row.y + (RowHeight - UIConstants.ButtonHeight) / 2f, loadWidth, UIConstants.ButtonHeight);
        right -= loadWidth + UIConstants.GapX;
        var overwriteBtn = new Rect(right - overwriteWidth, row.y + (RowHeight - UIConstants.ButtonHeight) / 2f, overwriteWidth, UIConstants.ButtonHeight);
        right -= overwriteWidth + UIConstants.GapX;
        var dateLabel = new Rect(right - DateColumnWidth, row.y + (RowHeight - Text.LineHeight) / 2f, DateColumnWidth, Text.LineHeight);
        right -= DateColumnWidth + UIConstants.GapX;
        var markLabel = new Rect(right - GetMarkColumnWidth(), row.y + (RowHeight - Text.LineHeight) / 2f, GetMarkColumnWidth(), Text.LineHeight);
        right -= GetMarkColumnWidth() + UIConstants.GapX;
        var nameRect = new Rect(row.x, row.y, Mathf.Max(right - row.x, NameColumnMinWidth), RowHeight);

        // 1st column — name.
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(nameRect, e.name);
        Text.Anchor = TextAnchor.UpperLeft;

        // Mark column — the policy's mark (cross-scope/cross-bucket) or empty.
        if (mark != null)
        {
            GUI.color = Color.cyan;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(markLabel, mark);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        // Date — Tiny font so it fits on one line (like the save/load window).
        Text.Font = GameFont.Tiny;
        GUI.color = Color.gray;
        Widgets.Label(dateLabel, e.date.ToString("yyyy-MM-dd HH:mm"));
        GUI.color = Color.white;
        Text.Font = GameFont.Small;

        // Overwrite button (left of Load), with confirmation.
        if (CanOverwrite(e))
        {
            if (Widgets.ButtonText(overwriteBtn, ASMKeys.OverwritePreset.Translate()))
            {
                var captured = e;
                Find.WindowStack.Add(new Dialog_MessageBox(ASMKeys.ConfirmOverwrite.Translate(captured.name),
                    ASMKeys.OverwritePreset.Translate(),
                    () => OverwriteEntry(captured),
                    "No".Translate()) { doCloseX = true });
            }
        }

        var tip = GetRowTip(e);

        if (tip != null)
        {
            TooltipHandler.TipRegion(row, tip);
        }

        if (Widgets.ButtonText(loadBtn, ASMKeys.LoadPreset.Translate()))
        {
            if (Apply(e))
            {
                Messages.Message(ASMKeys.PresetLoaded.Translate(e.name), MessageTypeDefOf.TaskCompletion, false);
            }
            else
            {
                Messages.Message(ASMKeys.PresetNoSlice.Translate(targetLabel), MessageTypeDefOf.RejectInput, false);
            }
        }

        if (CanDelete(e))
        {
            TooltipHandler.TipRegion(deleteBtn, ASMKeys.DeletePreset.Translate());

            if (Widgets.ButtonImage(deleteBtn, TexButton.Delete))
            {
                var captured = e;
                Find.WindowStack.Add(new Dialog_MessageBox(GetConfirmDeleteText(captured),
                    ASMKeys.DeletePreset.Translate(), () => PresetRepository.Delete(captured),
                    "No".Translate()) { doCloseX = true });
            }
        }
    }

    private void OverwriteEntry(PresetEntry e)
    {
        Save(e.name);
        Messages.Message(ASMKeys.PresetOverwritten.Translate(e.name), MessageTypeDefOf.TaskCompletion, false);
    }

    // ---- Helpers ----------------------------------------------------------------------------

    private static float GetButtonWidth(string text) =>
        Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(text).x + UIConstants.ButtonPaddingX);

    // The mark column fits the longest scope mark, with a floor.
    private static float GetMarkColumnWidth() =>
        Mathf.Max(MarkColumnMinWidth, Text.CalcSize(ASMKeys.PresetAllMark.Translate()).x + UIConstants.GapX);

    private List<PresetEntry> Gather((PresetScope Scope, IEnumerable<PresetEntry> Entries)[] sources)
    {
        var entries = new List<PresetEntry>();

        foreach (var source in sources)
        {
            // A single offered scope has no toggle and always contributes.
            if (sources.Length == 1 || !scopesState.TryGetValue(source.Scope, out var shown) || shown)
            {
                entries.AddRange(source.Entries);
            }
        }

        entries = entries.OrderByDescending(e => e.date).ThenBy(e => e.name).ToList();

        string f = filterBuffer.Trim();

        if (!f.NullOrEmpty())
        {
            entries = entries.Where(e => e.name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        return entries;
    }
}
