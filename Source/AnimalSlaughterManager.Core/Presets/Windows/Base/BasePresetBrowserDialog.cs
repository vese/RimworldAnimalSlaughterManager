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
    // TODO: посмотреть использования
    // Row heights follow the font, like the settings windows' controls.
    internal static readonly float RowHeight = UIConstants.ButtonHeight + 2f * UIConstants.TextPaddingY;

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

    private EntryRowController RowController =>
        field ??= new(GetRowMark, GetRowTip, CanOverwrite, CanDelete, GetConfirmDeleteText, Apply, OverwriteEntry, targetLabel);

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

    public override void DoWindowContents(Rect inRect)
    {
        var font = Text.Font;
        Text.Font = GameFont.Small;

        var top = inRect.y + UIConstants.GapX;
        var scopes = ScopeValues.Where(scope => ScopeSources.Any(s => s.Scope == scope)).ToList();

        top = SearchRow.Draw(inRect.x, top, inRect.width, scopes, ref scopesState, ref filterBuffer);

        // The preset list fills everything between the search row and the save row.
        var listTop = top + UIConstants.GapX;
        var listBottom = inRect.yMax - RowHeight - UIConstants.GapX;
        PresetList.Draw(inRect.x, listTop, inRect.width, listBottom - listTop, Gather(ScopeSources), filterBuffer, RowController, ref scroll, ref listHeight);

        SaveRow.Draw(inRect.x, inRect.yMax - RowHeight, inRect.width, HasData, Save, ref nameBuffer);

        Text.Font = font;
    }

    private void OverwriteEntry(PresetEntry e)
    {
        Save(e.name);
        Messages.Message(ASMKeys.PresetOverwritten.Translate(e.name), MessageTypeDefOf.TaskCompletion, false);
    }

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
