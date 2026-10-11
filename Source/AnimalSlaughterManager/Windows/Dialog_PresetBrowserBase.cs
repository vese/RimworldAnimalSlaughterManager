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
/// the bottom — the layout of RimWorld's save/load list. Subclasses provide the policy:
/// which entries each scope lists, the marks and tooltips, saving, applying and delete gating.
/// Rows always reserve the same set of columns, so they stay aligned when a button or mark is
/// absent. Entries sort newest-first and filter by name and by the scope toggles.
/// </summary>
public abstract class Dialog_PresetBrowserBase : Window
{
    private Vector2 scroll;
    private float listHeight = 9999f;
    private string nameBuffer = "";
    private string filterBuffer = "";

    protected bool showList = true, showKind = true, showAll = true;

    private const float Gap = 6f;     // consistent vertical gap (title→filter→rows→rows)
    private const float PadX = 8f;    // row left/right padding
    private const float RowH = 30f;

    public override Vector2 InitialSize => new Vector2(820f, 600f);

    protected Dialog_PresetBrowserBase()
    {
        doCloseX = true;
        draggable = true;
        resizeable = true;
    }

    // ---- Policy hooks ---------------------------------------------------------------------

    /// <summary>Entries of the List scope (the browser's own list flavor).</summary>
    protected abstract IEnumerable<PresetEntry> ListEntries();

    protected virtual IEnumerable<PresetEntry> KindEntries() => PresetRepository.ListPresets(PresetScope.Kind);

    protected virtual IEnumerable<PresetEntry> AllEntries() => PresetRepository.ListPresets(PresetScope.All);

    /// <summary>True when the target actually has data worth saving (enables the save row).</summary>
    protected abstract bool HasData();

    /// <summary>Saves the target under the name. The base clears the name field and reports.</summary>
    protected abstract void Save(string name);

    /// <summary>Applies the preset; false = the preset had no usable slice for this target.</summary>
    protected abstract bool Apply(PresetEntry entry);

    /// <summary>Label for the "no slice" reject message when <see cref="Apply"/> fails.</summary>
    protected abstract string NoSliceLabel();

    /// <summary>The row's mark-column content (cross-scope or cross-bucket mark); null = empty.</summary>
    protected virtual string? MarkOf(PresetEntry entry) => null;

    /// <summary>The row's tooltip; null = none.</summary>
    protected virtual string? RowTip(PresetEntry entry) => null;

    /// <summary>Whether the row carries the Overwrite button.</summary>
    protected virtual bool CanOverwrite(PresetEntry entry) => HasData();

    /// <summary>Whether the row carries the Delete button.</summary>
    protected virtual bool CanDelete(PresetEntry entry) => true;

    /// <summary>The delete-confirmation message for the entry.</summary>
    protected virtual string ConfirmDelete(PresetEntry entry) => ASMKeys.ConfirmDelete.Translate(entry.name);

    /// <summary>Whether the scope toggle row is shown at all (a single-type browser skips it).</summary>
    protected virtual bool ShowScopeToggles => true;

    /// <summary>Whether the scope's toggle is offered in this context.</summary>
    protected virtual bool ScopeVisible(PresetScope scope) => true;

    private bool ScopeShown(PresetScope s) => s == PresetScope.All ? showAll : s == PresetScope.Kind ? showKind : showList;

    // ---- Frame ----------------------------------------------------------------------------

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;

        float btnW = Mathf.Max(96f, Text.CalcSize(ASMKeys.SavePreset.Translate()).x + 18f);

        // --- Top: search row, one Gap below the title. Magnifier + filter field on the left,
        // clear ✕ after it; scope-type toggles right-aligned to the window edge. ---
        float y = inRect.y + Gap;
        const float topH = 28f;
        const float icon = 22f, gap = 6f, clearGap = 10f;
        float toggleW = 0f;

        if (ShowScopeToggles)
        {
            foreach (var sc in ScopeOrder)
            {
                if (ScopeVisible(sc))
                {
                    toggleW += ScopeToggleWidth(sc) + Gap;
                }
            }
        }

        float fieldEndBase = inRect.xMax - icon - clearGap;
        float mid = ShowScopeToggles ? inRect.xMax - toggleW : fieldEndBase;
        GUI.DrawTexture(new Rect(inRect.x, y + (topH - icon) / 2f, icon, icon), TexButton.Search);
        float fieldX = inRect.x + icon + gap;
        float fieldEnd = mid - icon - clearGap;
        filterBuffer = Widgets.TextField(new Rect(fieldX, y + 1f, fieldEnd - fieldX, 26f), filterBuffer);
        Rect clearBtn = new Rect(mid - icon, y + (topH - icon) / 2f, icon, icon);
        TooltipHandler.TipRegion(clearBtn, ASMKeys.Clear.Translate());

        if (Widgets.ButtonImage(clearBtn, TexButton.CloseXSmall))
        {
            filterBuffer = "";
        }

        if (ShowScopeToggles)
        {
            float tx = inRect.xMax;
            // Right-aligned: walk the scopes right-to-left.
            foreach (var sc in ScopeOrder)
            {
                if (!ScopeVisible(sc))
                {
                    continue;
                }

                float tw = ScopeToggleWidth(sc);
                tx -= tw;
                DrawScopeToggle(tx, y, tw, sc);

                tx -= Gap;
            }
        }

        y += topH + Gap;

        // --- Bottom: save row (name field + Save button). ---
        const float saveRowH = 30f;
        float listBottom = inRect.yMax - saveRowH - Gap;

        // --- Middle: the preset list. ---
        var entries = Gather();
        Rect outRect = new Rect(inRect.x, y, inRect.width, listBottom - y);
        Rect view = new Rect(inRect.x, y, inRect.width - 16f, Mathf.Max(listHeight, outRect.height));
        Widgets.BeginScrollView(outRect, ref scroll, view);

        float cy = view.y;

        if (entries.Count == 0)
        {
            GUI.color = Color.gray;
            Widgets.Label(new Rect(view.x, cy + 4f, view.width, 24f),
                filterBuffer.Trim().NullOrEmpty() ? ASMKeys.NoPresets.Translate() : ASMKeys.NoPresetsFiltered.Translate());
            GUI.color = Color.white;
            cy += 28f;
        }

        int i = 0;

        foreach (var e in entries)
        {
            Rect row = new Rect(view.x + PadX, cy, view.width - 2f * PadX, RowH);
            DrawEntryRow(row, e, i);
            cy += RowH + Gap;
            i++;
        }

        listHeight = Mathf.Max(cy - view.y, outRect.height);
        Widgets.EndScrollView();

        // Save row: name field + Save. Disabled when nothing to save or no name.
        bool hasData = HasData();
        bool canSave = hasData && !nameBuffer.Trim().NullOrEmpty();
        float sy = inRect.yMax - saveRowH + 2f;
        GUI.enabled = hasData;
        nameBuffer = Widgets.TextField(new Rect(inRect.x, sy, inRect.width - btnW - gap, 26f), nameBuffer);

        if (Widgets.ButtonText(new Rect(inRect.xMax - btnW, sy, btnW, 26f), ASMKeys.SavePreset.Translate(), active: canSave) && canSave)
        {
            string name = (nameBuffer ?? "").Trim();
            Save(name);
            nameBuffer = "";
            Messages.Message(ASMKeys.PresetSaved.Translate(name), MessageTypeDefOf.TaskCompletion, false);
        }

        GUI.enabled = true;
    }

    private static readonly PresetScope[] ScopeOrder = [PresetScope.List, PresetScope.Kind, PresetScope.All];

    private static float ScopeToggleWidth(PresetScope s)
    {
        string mark = ScopeMark(s);
        const float iconS = 20f;
        return iconS + 4f + Text.CalcSize(mark).x + 8f;
    }

    private static string ScopeMark(PresetScope s) => s == PresetScope.All ? ASMKeys.PresetAllMark.Translate()
        : s == PresetScope.Kind ? ASMKeys.PresetKindMark.Translate()
        : ASMKeys.PresetListMark.Translate();

    private void DrawScopeToggle(float x, float y, float w, PresetScope s)
    {
        bool on = ScopeShown(s);
        string mark = ScopeMark(s);
        const float iconS = 20f;
        Rect r = new Rect(x, y + 1f, w, 26f);
        Widgets.DrawHighlightIfMouseover(r);
        // Indicator: green check (shown) or red X (hidden).
        Rect iconRect = new Rect(r.x, r.y + (r.height - iconS) / 2f, iconS, iconS);

        if (on)
        {
            GUI.color = Color.green;
            Widgets.CheckboxDraw(iconRect.x, iconRect.y, true, false, iconS);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.red;
            GUI.DrawTexture(iconRect, TexButton.CloseXSmall);
            GUI.color = Color.white;
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(iconRect.xMax + 4f, r.y, r.width - iconS - 4f, r.height), mark);
        Text.Anchor = TextAnchor.UpperLeft;

        if (Widgets.ButtonInvisible(r))
        {
            if (s == PresetScope.All)
            {
                showAll = !showAll;
            }
            else if (s == PresetScope.Kind)
            {
                showKind = !showKind;
            }
            else
            {
                showList = !showList;
            }
        }
    }

    private List<PresetEntry> Gather()
    {
        var entries = new List<PresetEntry>();

        // A toggle only contributes when its scope is offered in this context at all.
        if (showList && ScopeVisible(PresetScope.List))
        {
            entries.AddRange(ListEntries());
        }

        if (showKind && ScopeVisible(PresetScope.Kind))
        {
            entries.AddRange(KindEntries());
        }

        if (showAll && ScopeVisible(PresetScope.All))
        {
            entries.AddRange(AllEntries());
        }

        entries = entries.OrderByDescending(e => e.date).ThenBy(e => e.name).ToList();

        string f = filterBuffer.Trim();

        if (!f.NullOrEmpty())
        {
            entries = entries.Where(e => e.name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        return entries;
    }

    private void DrawEntryRow(Rect row, PresetEntry e, int index)
    {
        // Zebra rows, like the save/load list.
        if (index % 2 == 1)
        {
            Widgets.DrawAltRect(row);
        }

        string mark = MarkOf(e);

        // Columns always reserved right-to-left. Overwrite (left of Load) only shown
        // for rows allowed by the policy when there is data to overwrite with.
        float loadW = Mathf.Max(96f, Text.CalcSize(ASMKeys.LoadPreset.Translate()).x + 18f);
        float overW = Mathf.Max(96f, Text.CalcSize(ASMKeys.OverwritePreset.Translate()).x + 18f);
        float extraW = Mathf.Max(80f, Text.CalcSize(ASMKeys.PresetAllMark.Translate()).x + 6f);
        const float delW = 26f, dateW = 86f, gap = 6f;

        float right = row.xMax;
        Rect delRect = new Rect(right - delW, row.y + 2f, delW, 26f);
        right -= delW + gap;
        Rect loadBtn = new Rect(right - loadW, row.y + 2f, loadW, 26f);
        right -= loadW + gap;
        Rect overBtn = new Rect(right - overW, row.y + 2f, overW, 26f);
        right -= overW + gap;
        Rect dateRect = new Rect(right - dateW, row.y + 4f, dateW, 22f);
        right -= dateW + gap;
        Rect extraRect = new Rect(right - extraW, row.y + 4f, extraW, 22f);
        right -= extraW + gap;
        Rect nameRect = new Rect(row.x, row.y + 5f, Mathf.Max(right - row.x, 40f), 22f);

        // 1st column — name.
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(nameRect, e.name);
        Text.Anchor = TextAnchor.UpperLeft;

        // Extra column — the policy's mark (cross-scope/cross-bucket) or empty.
        if (mark != null)
        {
            GUI.color = Color.cyan;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(extraRect, mark);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        // Date — Tiny font so it fits on one line (like the save/load window).
        Text.Font = GameFont.Tiny;
        GUI.color = Color.gray;
        Widgets.Label(dateRect, e.date.ToString("yyyy-MM-dd HH:mm"));
        GUI.color = Color.white;
        Text.Font = GameFont.Small;

        // Overwrite button (left of Load), with confirmation.
        if (CanOverwrite(e))
        {
            if (Widgets.ButtonText(overBtn, ASMKeys.OverwritePreset.Translate()))
            {
                var captured = e;
                Find.WindowStack.Add(new Dialog_MessageBox(ASMKeys.ConfirmOverwrite.Translate(captured.name),
                    ASMKeys.OverwritePreset.Translate(), () => OverwriteEntry(captured),
                    "No".Translate()) { doCloseX = true });
            }
        }

        var tip = RowTip(e);

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
                Messages.Message(ASMKeys.PresetNoSlice.Translate(NoSliceLabel()), MessageTypeDefOf.RejectInput, false);
            }
        }

        if (CanDelete(e))
        {
            TooltipHandler.TipRegion(delRect, ASMKeys.DeletePreset.Translate());

            if (Widgets.ButtonImage(delRect, TexButton.Delete))
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
}
