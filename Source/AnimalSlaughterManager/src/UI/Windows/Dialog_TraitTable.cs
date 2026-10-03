using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// The shared Animal Traits System trait table: sortable (good/bad + one column per stat/
/// capacity modifier), searchable, with drag-paint row selection (like the Pets tab), and
/// reorderable/hideable columns (drag the header handle, ✕ to hide).
/// Subclasses define the check column and what "Add selected" does.
/// </summary>
public abstract class Dialog_TraitTable : Window
{
    protected enum ColKind { Name, Type, Stat, Cap }

    protected class Col(ColKind kind, float width, string header, string key)
    {
        public ColKind kind = kind;
        public StatDef? stat;
        public PawnCapacityDef? cap;
        public float width = width;
        public string header = header;
        public string key = key;
    }

    protected class Row(HediffDef def)
    {
        public HediffDef def = def;
        public bool isBad = def.isBad;
        public Dictionary<StatDef, float> statValues = new Dictionary<StatDef, float>();
        public Dictionary<StatDef, string> statStrings = new Dictionary<StatDef, string>();
        public Dictionary<PawnCapacityDef, float> capValues = new Dictionary<PawnCapacityDef, float>();
        public Dictionary<PawnCapacityDef, string> capStrings = new Dictionary<PawnCapacityDef, string>();
    }

    protected const float NameW = 280f;

    private const float TypeW = 70f;
    private const float ModW = 90f;
    private const float HeaderTopH = UIConstants.IconSize;
    private const float HeaderSubH = 24f;
    private const float HeaderH = HeaderTopH + HeaderSubH;
    private const float RowH = 28f;
    private const float TopPad = 12f;             // keeps the scrollbar clear of the window's close-X
    private const float BottomBarHeight = 40f;    // the Add-selected bar under the table
    private const float AddButtonWidth = 220f;
    private const float ShowAllButtonWidth = 180f;
    private const float SmallIconSize = 16f;      // TexButton.CloseXSmall

    // Cached UI tint colors (struct, but cached to avoid rebuilding each draw + dedup).
    private static readonly Color NegativeColor = new Color(1f, 0.5f, 0.5f);
    private static readonly Color PositiveColor = new Color(0.5f, 1f, 0.5f);
    private static readonly Color SortColumnTint = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color RowAltTint = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color DarkPanelBg = new Color(0.16f, 0.16f, 0.16f, 0.97f);

    private readonly List<Row> rows;
    private readonly List<Col> columns = new List<Col>();
    private readonly HashSet<string> hidden = new HashSet<string>();
    protected readonly HashSet<HediffDef> selected = new HashSet<HediffDef>();
    private int sortIndex = 0;
    private bool sortAsc = true;
    private int colGroup = -1;
    private Vector2 scroll;
    private string searchBuffer = "";

    // Drag-paint selection state (shared across rows like the vanilla animal-tab checkboxes).
    protected bool paintMode;
    protected bool paintValue;

    public override Vector2 InitialSize => new Vector2(1120f, 640f);

    protected Dialog_TraitTable()
    {
        doCloseX = true;
        // Not draggable: a draggable window's GUI.DragWindow() grabs any unclaimed press, so
        // dragging a column would move the window instead. Non-draggable lets column reorder
        // work reliably. (Opens centered; resizeable.)
        draggable = false;
        resizeable = true;
        absorbInputAroundWindow = true;

        rows = AnimalTraitsAccess.KnownTraitDefs.Select(MakeRow).ToList();
        var statCols = rows.SelectMany(r => r.statValues.Keys).Distinct().OrderBy(s => s.label).ToList();
        var capCols = rows.SelectMany(r => r.capValues.Keys).Distinct().OrderBy(c => c.label).ToList();

        columns.Add(new Col(ColKind.Name, NameW, ASMKeys.Trait.Translate(), "name"));
        columns.Add(new Col(ColKind.Type, TypeW, ASMKeys.TypeCol.Translate(), "type"));
        foreach (var s in statCols)
            columns.Add(new Col(ColKind.Stat, ModW, s.LabelCap, s.defName) { stat = s });
        foreach (var c in capCols)
            columns.Add(new Col(ColKind.Cap, ModW, c.LabelCap, c.defName) { cap = c });
    }

    /// <summary>Width of the leading column the subclass draws in front of the Name column.</summary>
    protected abstract float LeadingColumnWidth { get; }

    /// <summary>Called when "Add selected" is pressed with a non-empty selection.</summary>
    protected abstract void Confirm();

    /// <summary>Optional content in the header's leading-column area (e.g. the ✓/✗ pair).</summary>
    protected virtual void DrawLeadingHeaderIcons(Rect r) { }

    private static Row MakeRow(HediffDef def)
    {
        var r = new Row(def);
        // Aggregate modifiers across ALL stages (not just the first) so multi-stage traits — e.g.
        // from ATS Extended or other AnimalTrait_* mods — show every stat/capacity they affect.
        if (def.stages != null)
        {
            foreach (var stage in def.stages)
            {
                if (stage == null) continue;
                if (stage.statOffsets != null)
                    foreach (var sm in stage.statOffsets)
                    {
                        r.statValues[sm.stat] = sm.value;
                        r.statStrings[sm.stat] = sm.ValueToStringAsOffset;
                    }
                if (stage.statFactors != null)
                    foreach (var sm in stage.statFactors)
                    {
                        // Store the delta (factor - 1) so sorting by magnitude works across offset/factor traits;
                        // display as a signed percent via the shared formatter.
                        r.statValues[sm.stat] = sm.value - 1f;
                        r.statStrings[sm.stat] = AnimalTraitsAccess.FormatStatFactor(sm.value);
                    }
                if (stage.capMods != null)
                    foreach (var cm in stage.capMods)
                    {
                        r.capValues[cm.capacity] = cm.offset;
                        r.capStrings[cm.capacity] = cm.offset.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Offset);
                    }
            }
        }
        return r;
    }

    private List<Col> VisibleColumns => columns.Where(c => !hidden.Contains(c.key)).ToList();

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;

        // Search row: magnifier + field (left half) + clear ✕, one band below the close-X margin.
        float searchH = UIConstants.ButtonHeight + 2f * UIConstants.TextPaddingY;
        float searchY = inRect.y + TopPad;
        float mid = inRect.x + inRect.width * 0.5f;
        float icon = UIConstants.IconSize;
        float clearGap = UIConstants.GapY + UIConstants.TextPaddingY;
        GUI.DrawTexture(new Rect(inRect.x, searchY + (searchH - icon) / 2f, icon, icon), TexButton.Search);
        float fx = inRect.x + icon + UIConstants.GapX;
        float fieldEnd = mid - icon - clearGap;
        searchBuffer = Widgets.TextField(new Rect(fx, searchY + 1f, fieldEnd - fx, UIConstants.ButtonHeight), searchBuffer);
        Rect clearBtn = new Rect(mid - icon, searchY + (searchH - icon) / 2f, icon, icon);
        TooltipHandler.TipRegion(clearBtn, ASMKeys.Clear.Translate());
        if (Widgets.ButtonImage(clearBtn, TexButton.CloseXSmall))
            searchBuffer = "";

        float tableTop = searchY + searchH + UIConstants.GapX;

        var vis = VisibleColumns;
        float tableW = LeadingColumnWidth + vis.Sum(c => c.width);
        var sorted = SortedRows();
        string sf = (searchBuffer ?? "").Trim();
        var visibleRows = sf.NullOrEmpty() ? sorted : sorted.Where(r => r.def.LabelCap.ToString().IndexOf(sf, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        float contentH = HeaderH + visibleRows.Count * RowH + 4f;
        Rect outRect = new Rect(inRect.x, tableTop, inRect.width, inRect.yMax - BottomBarHeight - tableTop);
        Rect view = new Rect(inRect.x, tableTop, Mathf.Max(tableW, outRect.width), Mathf.Max(contentH, outRect.height));
        Widgets.BeginScrollView(outRect, ref scroll, view);

        DrawHeader(new Rect(view.x, view.y, view.width, HeaderH), vis);
        DrawLeadingHeaderIcons(new Rect(view.x, view.y, view.width, HeaderH));

        float cy = view.y + HeaderH;
        for (int i = 0; i < visibleRows.Count; i++)
        {
            Rect rowRect = new Rect(view.x, cy, view.width, RowH);
            if (i % 2 == 1) Widgets.DrawAltRect(rowRect);
            DrawRow(rowRect, visibleRows[i], vis);
            cy += RowH;
        }
        Widgets.EndScrollView();

        // Reset paint state if the mouse was released anywhere.
        if (Event.current.type == EventType.MouseUp) paintMode = false;

        // Bottom bar: add selected + show all columns.
        float by = inRect.yMax - BottomBarHeight + UIConstants.GapY;
        bool any = selected.Count > 0;
        var addBtn = new Rect(inRect.x, by, AddButtonWidth, UIConstants.ButtonHeight);
        if (Widgets.ButtonText(addBtn, ASMKeys.AddSelected.Translate(selected.Count), active: any) && any)
        {
            Confirm();
            Close();
        }
        if (Widgets.ButtonText(new Rect(addBtn.xMax + UIConstants.GapY, by, ShowAllButtonWidth, UIConstants.ButtonHeight), ASMKeys.ShowAllColumns.Translate()))
            hidden.Clear();
    }

    // The window is not draggable as a whole (that made column reorder grab the window and jump).
    // Instead it is draggable from the window FRAME on all four sides plus the content areas
    // OUTSIDE the table (top pad + bottom bar). The table itself never starts a window drag, so
    // column reordering works cleanly with no conflict.
    protected override void LateWindowOnGUI(Rect inRect)
    {
        float m = inRect.x;                        // == Window.StandardMargin (the frame width)
        float winW = inRect.xMax + m;
        float winH = inRect.yMax + m;
        GUI.DragWindow(new Rect(0, 0, winW, m));                                  // top frame
        GUI.DragWindow(new Rect(0, winH - m, winW, m));                           // bottom frame
        GUI.DragWindow(new Rect(0, 0, m, winH));                                  // left frame
        GUI.DragWindow(new Rect(winW - m, 0, m, winH));                           // right frame
        GUI.DragWindow(new Rect(inRect.x, inRect.y, inRect.width, TopPad));          // top pad (above table)
        GUI.DragWindow(new Rect(inRect.x, inRect.yMax - BottomBarHeight, inRect.width, BottomBarHeight)); // bottom bar (below table)
    }

    private void DrawHeader(Rect r, List<Col> vis)
    {
        // Only non-Name columns are reorderable; the Name column stays pinned first.
        var reorderableCols = vis.Where(c => c.kind != ColKind.Name).ToList();
        if (Event.current.type == EventType.Repaint)
        {
            var captured = reorderableCols;
            colGroup = ReorderableWidget.NewGroup((a, b) => ReorderColumns(captured, a, b), ReorderableDirection.Horizontal, r);
        }

        // Top row: clickable column names (click = sort). Truncated; the hovered one is redrawn
        // full (overflowing onto the columns to the right) at the end so it sits on top.
        var topCells = new List<(Rect rect, Col col, int idx)>();
        float x = r.x + LeadingColumnWidth;
        foreach (var col in vis)
        {
            int colIdx = columns.IndexOf(col);
            Rect top = new Rect(x, r.y, col.width, HeaderTopH);
            Widgets.DrawHighlightIfMouseover(top);
            if (Widgets.ButtonInvisible(top)) SetSort(colIdx);
            bool wrap = Text.WordWrap;
            Text.WordWrap = false;
            Text.Anchor = TextAnchor.MiddleCenter;
            bool isSort = colIdx == sortIndex;
            GUI.color = isSort ? Color.cyan : Color.white;
            Widgets.Label(top, col.header.Truncate(col.width - 4f));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = wrap;
            topCells.Add((top, col, colIdx));
            x += col.width;
        }

        // Sub row: Name = sort button only; others = sort button + drag handle + hide button.
        x = r.x + LeadingColumnWidth;
        for (int i = 0; i < vis.Count; i++)
        {
            var col = vis[i];
            int colIdx = columns.IndexOf(col);
            bool isName = col.kind == ColKind.Name;
            Rect sub = new Rect(x, r.y + HeaderTopH, col.width, HeaderSubH);

            // Sort button.
            bool isSort = colIdx == sortIndex;
            Rect sortBtn = new Rect(sub.x, sub.y, UIConstants.IconSize, HeaderSubH);
            TooltipHandler.TipRegion(sortBtn, ASMKeys.SortBy.Translate());
            if (Widgets.ButtonInvisible(sortBtn)) SetSort(colIdx);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = isSort ? Color.cyan : SortColumnTint;
            Widgets.Label(sortBtn, isSort ? (sortAsc ? "▲" : "▼") : "↕");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // Hide button (right). Name column is not hideable.
            float hideW = isName ? 0f : SmallIconSize + UIConstants.ButtonPaddingY;
            if (!isName)
            {
                Rect hideBtn = new Rect(sub.xMax - hideW, sub.y + (HeaderSubH - SmallIconSize) / 2f, SmallIconSize, SmallIconSize);
                TooltipHandler.TipRegion(hideBtn, ASMKeys.HideColumn.Translate());
                GUI.color = Color.red;
                if (Widgets.ButtonImage(hideBtn, TexButton.CloseXSmall))
                {
                    if (hidden.Contains(col.key)) hidden.Remove(col.key); else hidden.Add(col.key);
                }
                GUI.color = Color.white;
            }

            // Drag handle (middle) — only for non-Name columns. Claims the press so a draggable
            // window doesn't move instead of reordering the column.
            if (!isName)
            {
                // Drag handle (middle) — this is what reorders the column. The window never
                // starts a drag from the table (see LateWindowOnGUI), so no ClaimDragHandle needed.
                Rect drag = new Rect(sortBtn.xMax, sub.y, Mathf.Max(sub.xMax - hideW - sortBtn.xMax, UIConstants.GapY), HeaderSubH);
                ReorderableWidget.Reorderable(colGroup, drag);
                GUI.color = RowAltTint;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(drag, "↔");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            x += col.width;
        }

        // Gray gridlines between columns, between the name row and the control row, and under the header.
        GUI.color = Color.gray;
        x = r.x + LeadingColumnWidth;
        foreach (var col in vis)
        {
            Widgets.DrawLineVertical(x, r.y, r.height);
            x += col.width;
        }
        Widgets.DrawLineVertical(x, r.y, r.height);
        Widgets.DrawLineHorizontal(r.x, r.y + HeaderTopH, r.width);
        Widgets.DrawLineHorizontal(r.x, r.yMax, r.width);
        GUI.color = Color.white;

        // Hovered header: if the name is truncated, redraw the full text overflowing to the
        // right on top of a backdrop only as wide as the text (not the whole remaining header,
        // which would shade every column to the right).
        foreach (var tc in topCells)
        {
            if (Mouse.IsOver(tc.rect))
            {
                float textW = Text.CalcSize(tc.col.header).x + UIConstants.GapY;
                if (textW > tc.rect.width)
                {
                    Rect overRect = new Rect(tc.rect.x, tc.rect.y, textW, tc.rect.height);
                    GUI.color = DarkPanelBg;
                    GUI.DrawTexture(overRect, Texture2D.whiteTexture);
                    GUI.color = tc.idx == sortIndex ? Color.cyan : Color.white;
                    bool wrap = Text.WordWrap;
                    Text.WordWrap = false;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(overRect, tc.col.header);
                    Text.WordWrap = wrap;
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                }
                break;
            }
        }
    }

    private void ReorderColumns(List<Col> reorderableCols, int from, int to)
    {
        // `from`/`to` index the non-Name columns (Name is never registered as reorderable).
        if (from < 0 || from >= reorderableCols.Count || to < 0 || to > reorderableCols.Count || from == to) return;
        int ia = columns.IndexOf(reorderableCols[from]);
        var item = columns[ia];
        columns.RemoveAt(ia);
        // The target index is looked up AFTER the removal, so it already reflects the shift —
        // do NOT subtract again. (The old ib-- was the off-by-one: right-by-1 didn't move,
        // right-by-2 only moved by 1.)
        int ib;
        if (to == reorderableCols.Count)
            ib = columns.Count;                        // dropped past the last column → append
        else
            ib = columns.IndexOf(reorderableCols[to]); // post-removal index = correct insert slot
        columns.Insert(ib, item);
    }

    /// <summary>Draws the data columns of a row, starting at the row's left edge. Subclasses
    /// prepend their leading column (selection checks) and call this with the shifted rect.</summary>
    protected virtual void DrawRow(Rect row, Row r, List<Col> vis)
    {
        float x = row.x;
        foreach (var col in vis)
        {
            Rect cell = new Rect(x, row.y, col.width, row.height);
            DrawCell(cell, r, col);
            x += col.width;
        }
    }

    private static void DrawCell(Rect cell, Row r, Col col)
    {
        switch (col.kind)
        {
            case ColKind.Name:
                GUI.color = AnimalTraitsAccess.TraitColor(r.def);
                Text.Anchor = TextAnchor.MiddleLeft;
                bool wrap = Text.WordWrap;
                Text.WordWrap = false;
                Widgets.Label(new Rect(cell.x + 2f, cell.y, cell.width - 2f, cell.height), r.def.LabelCap);
                Text.WordWrap = wrap;
                GUI.color = Color.white;
                TooltipHandler.TipRegion(cell, AnimalTraitsAccess.TraitTip(r.def));
                break;
            case ColKind.Type:
                GUI.color = r.isBad ? NegativeColor : PositiveColor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(cell, (r.isBad ? ASMKeys.Bad : ASMKeys.Good).Translate());
                GUI.color = Color.white;
                break;
            case ColKind.Stat:
                Text.Anchor = TextAnchor.MiddleCenter;
                if (col.stat != null && r.statStrings.TryGetValue(col.stat, out string sv))
                {
                    GUI.color = r.statValues[col.stat] >= 0 ? PositiveColor : NegativeColor;
                    Widgets.Label(cell, sv);
                    GUI.color = Color.white;
                }
                break;
            case ColKind.Cap:
                Text.Anchor = TextAnchor.MiddleCenter;
                if (col.cap != null && r.capStrings.TryGetValue(col.cap, out string cv))
                {
                    GUI.color = r.capValues[col.cap] >= 0 ? PositiveColor : NegativeColor;
                    Widgets.Label(cell, cv);
                    GUI.color = Color.white;
                }
                break;
        }
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void SetSort(int index)
    {
        if (index < 0) return;
        if (index == sortIndex) sortAsc = !sortAsc;
        else { sortIndex = index; sortAsc = true; }
    }

    private List<Row> SortedRows()
    {
        if (sortIndex < 0 || sortIndex >= columns.Count) return rows.ToList();
        var col = columns[sortIndex];
        IOrderedEnumerable<Row> ordered;
        switch (col.kind)
        {
            case ColKind.Type:
                ordered = rows.OrderBy(r => r.isBad ? 1 : 0);
                break;
            case ColKind.Stat when col.stat is StatDef stat:
                ordered = rows.OrderBy(r => r.statValues.TryGetValue(stat, out float v) ? v : 0f);
                break;
            case ColKind.Cap when col.cap is PawnCapacityDef cap:
                ordered = rows.OrderBy(r => r.capValues.TryGetValue(cap, out float v) ? v : 0f);
                break;
            default:
                ordered = rows.OrderBy(r => r.def.LabelCap.ToString());
                break;
        }
        return sortAsc ? ordered.ToList() : ordered.Reverse().ToList();
    }
}
