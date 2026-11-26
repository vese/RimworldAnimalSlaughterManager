using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;
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
    private const float ClearIconGap = 10f;    // search field ↔ the ✕ (or the toggles)
    private const float ToggleIconGap = 4f;    // a toggle's icon ↔ its label
    private const float DateColumnWidth = 86f;
    private const float DeleteColumnWidth = 26f;
    private const float MarkColumnMinWidth = 80f;
    private const float NameColumnMinWidth = 40f;

    // Row heights follow the font, like the settings windows' controls.
    private static float TopRowHeight => UIConstants.ButtonHeight + UIConstants.TextPaddingY;
    private static float RowHeight => UIConstants.ButtonHeight + 2f * UIConstants.TextPaddingY;

    private static readonly PresetScope[] ScopeOrder = [PresetScope.List, PresetScope.Kind, PresetScope.All];

    private Vector2 scroll;
    private float listHeight = 9999f;
    private string nameBuffer = string.Empty;
    private string filterBuffer = string.Empty;
    private readonly Dictionary<PresetScope, bool> showScope = [];

    /// <summary>The target the presets apply to (a kind's or bucket's label): the argument the
    /// subclass's title key translates with and the name the no-slice message quotes.</summary>
    protected TaggedString targetLabel;

    public override Vector2 InitialSize => new Vector2(820f, 600f);

    protected BasePresetBrowserDialog()
    {
        doCloseX = true;
        draggable = true;
        resizeable = true;
    }

    // ---- Policy hooks ---------------------------------------------------------------------

    /// <summary>The scopes this browser offers with their entry sources (its own scope plus
    /// any broader ones it imports from). One scope — no toggle strip.</summary>
    protected abstract (PresetScope Scope, IEnumerable<PresetEntry> Entries)[] ScopeSources { get; }

    /// <summary>True when the target actually has data worth saving (enables the save row).</summary>
    protected abstract bool HasData { get; }

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
    protected virtual string ConfirmDelete(PresetEntry entry) => ASMKeys.ConfirmDelete.Translate(entry.name);

    // ---- Frame ----------------------------------------------------------------------------

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;

        var top = inRect.y + UIConstants.GapX;

        DrawSearchRow(inRect.x, top, inRect.width);

        // The preset list fills everything between the search row and the save row.
        var listTop = top + TopRowHeight + UIConstants.GapX;
        var listBottom = inRect.yMax - RowHeight - UIConstants.GapX;
        DrawPresetList(inRect.x, listTop, inRect.width, listBottom - listTop);

        DrawSaveRow(inRect.x, inRect.yMax - RowHeight, inRect.width);
    }

    // Top row: magnifier + filter field on the left, clear ✕ after it; scope-type toggles
    // right-aligned to the window edge.
    private void DrawSearchRow(float x, float y, float width)
    {
        bool toggles = ScopeSources.Length > 1;
        float mid = x + width - (toggles ? GetScopeTogglesWidth() : UIConstants.IconSize + ClearIconGap);
        float iconY = y + (TopRowHeight - UIConstants.IconSize) / 2f;

        GUI.DrawTexture(new Rect(x, iconY, UIConstants.IconSize, UIConstants.IconSize), TexButton.Search);

        float fieldX = x + UIConstants.IconSize + UIConstants.GapX;
        float fieldEnd = mid - UIConstants.IconSize - ClearIconGap;
        filterBuffer = Widgets.TextField(new Rect(fieldX, y + (TopRowHeight - UIConstants.ButtonHeight) / 2f, fieldEnd - fieldX, UIConstants.ButtonHeight), filterBuffer);

        var clearBtn = new Rect(mid - UIConstants.IconSize, iconY, UIConstants.IconSize, UIConstants.IconSize);
        TooltipHandler.TipRegion(clearBtn, ASMKeys.Clear.Translate());

        if (Widgets.ButtonImage(clearBtn, TexButton.CloseXSmall))
        {
            filterBuffer = string.Empty;
        }

        if (toggles)
        {
            DrawScopeToggles(x + width, y);
        }
    }

    private void DrawScopeToggles(float rightEdge, float y)
    {
        float right = rightEdge;

        // Right-aligned: walk the scopes right-to-left.
        foreach (var scope in ScopeOrder)
        {
            if (!ScopeSources.Any(s => s.Scope == scope))
            {
                continue;
            }

            right -= GetScopeToggleWidth(scope);
            DrawScopeToggle(right, y, GetScopeToggleWidth(scope), scope);

            right -= UIConstants.GapX;
        }
    }

    private void DrawScopeToggle(float x, float y, float width, PresetScope scope)
    {
        bool on = !showScope.TryGetValue(scope, out var shown) || shown;
        var rect = new Rect(x, y + (TopRowHeight - UIConstants.ButtonHeight) / 2f, width, UIConstants.ButtonHeight);
        Widgets.DrawHighlightIfMouseover(rect);

        // Indicator: green check (shown) or red X (hidden).
        var iconRect = new Rect(rect.x, rect.y + (rect.height - UIConstants.IconSize) / 2f, UIConstants.IconSize, UIConstants.IconSize);

        if (on)
        {
            GUI.color = Color.green;
            Widgets.CheckboxDraw(iconRect.x, iconRect.y, true, false, UIConstants.IconSize);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.red;
            GUI.DrawTexture(iconRect, TexButton.CloseXSmall);
            GUI.color = Color.white;
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(iconRect.xMax + ToggleIconGap, rect.y, rect.width - UIConstants.IconSize - ToggleIconGap, rect.height), GetScopeMark(scope));
        Text.Anchor = TextAnchor.UpperLeft;

        if (Widgets.ButtonInvisible(rect))
        {
            showScope[scope] = !on;
        }
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
                Find.WindowStack.Add(new Dialog_MessageBox(ConfirmDelete(captured),
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

    private float GetScopeTogglesWidth()
    {
        float width = 0f;

        foreach (var scope in ScopeOrder)
        {
            if (ScopeSources.Any(s => s.Scope == scope))
            {
                width += GetScopeToggleWidth(scope) + UIConstants.GapX;
            }
        }

        return width;
    }

    private static float GetScopeToggleWidth(PresetScope scope) =>
        UIConstants.IconSize + ToggleIconGap + Text.CalcSize(GetScopeMark(scope)).x + UIConstants.GapX;

    private static string GetScopeMark(PresetScope scope) => scope == PresetScope.All ? ASMKeys.PresetAllMark.Translate()
        : scope == PresetScope.Kind ? ASMKeys.PresetKindMark.Translate()
        : ASMKeys.PresetListMark.Translate();

    private List<PresetEntry> Gather((PresetScope Scope, IEnumerable<PresetEntry> Entries)[] sources)
    {
        var entries = new List<PresetEntry>();

        foreach (var source in sources)
        {
            // A single offered scope has no toggle and always contributes.
            if (sources.Length == 1 || !showScope.TryGetValue(source.Scope, out var shown) || shown)
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
