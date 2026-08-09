using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM
{
    /// <summary>
    /// Animal Traits System trait picker: a sortable table (good/bad + one column per stat/
    /// capacity modifier). Multi-select traits to add several at once — click a row's checkbox
    /// and drag across rows to paint the same state on several traits (like the Pets tab).
    ///
    /// A search field at the top (magnifier + half-width input + clear ✕) filters the table by
    /// trait name. The header is two rows: the column name on top, and below it a sort button, a
    /// drag handle (reorder the column) and a hide button. Columns can thus be reordered by
    /// dragging and shown/hidden by clicking ✕.
    /// </summary>
    public class Dialog_TraitPicker : Window
    {
        private enum ColKind { Name, Type, Stat, Cap }

        private class Col
        {
            public ColKind kind;
            public StatDef stat;
            public PawnCapacityDef cap;
            public float width;
            public string header;
            public string key;
        }

        private class Row
        {
            public HediffDef def;
            public bool isBad;
            public Dictionary<StatDef, float> statValues = new Dictionary<StatDef, float>();
            public Dictionary<StatDef, string> statStrings = new Dictionary<StatDef, string>();
            public Dictionary<PawnCapacityDef, float> capValues = new Dictionary<PawnCapacityDef, float>();
            public Dictionary<PawnCapacityDef, string> capStrings = new Dictionary<PawnCapacityDef, string>();
        }

        private readonly Action<List<HediffDef>> onPicked;
        private readonly Action<List<(HediffDef, bool)>> onPickedFlag;
        private readonly bool twoButtonMode;
        private readonly Dictionary<HediffDef, bool> selFlags = new Dictionary<HediffDef, bool>();
        private readonly List<Row> rows;
        private readonly List<Col> columns = new List<Col>();
        private readonly HashSet<string> hidden = new HashSet<string>();
        private readonly HashSet<HediffDef> selected = new HashSet<HediffDef>();
        private int sortIndex = 0;
        private bool sortAsc = true;
        private int colGroup = -1;
        private Vector2 scroll;
        private string searchBuffer = "";

        // Drag-paint selection state (shared across rows like the vanilla animal-tab checkboxes).
        private bool paintMode;
        private bool paintValue;

        private const float CheckW = 30f;
        private const float NameW = 280f;
        private const float TypeW = 70f;
        private const float ModW = 90f;
        private const float HeaderTopH = 22f;
        private const float HeaderSubH = 24f;
        private const float HeaderH = HeaderTopH + HeaderSubH;
        private const float RowH = 28f;

        public override Vector2 InitialSize => new Vector2(1120f, 640f);

        public Dialog_TraitPicker(Action<List<HediffDef>> onPicked)
        {
            this.onPicked = onPicked;
            doCloseX = true;
            // Not draggable: a draggable window's GUI.DragWindow() grabs any unclaimed press, so
            // dragging a column would move the window instead. Non-draggable lets column reorder
            // work reliably. (Opens centered; resizeable.)
            draggable = false;
            resizeable = true;
            absorbInputAroundWindow = true;

            rows = AnimalTraitsAccess.KnownTraitDefs().Select(MakeRow).ToList();
            var statCols = rows.SelectMany(r => r.statValues.Keys).Distinct().OrderBy(s => s.label).ToList();
            var capCols = rows.SelectMany(r => r.capValues.Keys).Distinct().OrderBy(c => c.label).ToList();

            columns.Add(new Col { kind = ColKind.Name, width = NameW, header = ASMKeys.Trait.Translate(), key = "name" });
            columns.Add(new Col { kind = ColKind.Type, width = TypeW, header = ASMKeys.TypeCol.Translate(), key = "type" });
            foreach (var s in statCols)
                columns.Add(new Col { kind = ColKind.Stat, stat = s, width = ModW, header = s.LabelCap, key = s.defName });
            foreach (var c in capCols)
                columns.Add(new Col { kind = ColKind.Cap, cap = c, width = ModW, header = c.LabelCap, key = c.defName });
        }

        // Two-button constructor for condition lists: each trait has a green ✓ (has) and red ✗ (missing).
        public Dialog_TraitPicker(Action<List<(HediffDef, bool)>> onPickedFlag) : this((List<HediffDef> _) => { })
        {
            this.onPickedFlag = onPickedFlag;
            twoButtonMode = true;
        }

        private static Row MakeRow(HediffDef def)
        {
            var r = new Row { def = def, isBad = def.isBad };
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
            const float topPad = 12f; // keep the scrollbar clear of the window's close-X
            const float searchH = 30f;
            const float searchGap = 6f;
            float searchY = inRect.y + topPad;
            float mid = inRect.x + inRect.width * 0.5f;
            const float icon = 22f, gap = 6f, clearGap = 10f;
            GUI.DrawTexture(new Rect(inRect.x, searchY + (searchH - icon) / 2f, icon, icon), TexButton.Search);
            float fx = inRect.x + icon + gap;
            float fieldEnd = mid - icon - clearGap;
            searchBuffer = Widgets.TextField(new Rect(fx, searchY + 1f, fieldEnd - fx, 26f), searchBuffer);
            Rect clearBtn = new Rect(mid - icon, searchY + (searchH - icon) / 2f, icon, icon);
            TooltipHandler.TipRegion(clearBtn, ASMKeys.Clear.Translate());
            if (Widgets.ButtonImage(clearBtn, TexButton.CloseXSmall))
                searchBuffer = "";

            float tableTop = searchY + searchH + searchGap;

            var vis = VisibleColumns;
            float effectiveCheckW = twoButtonMode ? 44f : CheckW;
            float tableW = effectiveCheckW + vis.Sum(c => c.width);
            var sorted = SortedRows();
            string sf = (searchBuffer ?? "").Trim();
            var visibleRows = sf.NullOrEmpty() ? sorted : sorted.Where(r => r.def.LabelCap.ToString().IndexOf(sf, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            float contentH = HeaderH + visibleRows.Count * RowH + 4f;
            Rect outRect = new Rect(inRect.x, tableTop, inRect.width, inRect.yMax - 40f - tableTop);
            Rect view = new Rect(inRect.x, tableTop, Mathf.Max(tableW, outRect.width), Mathf.Max(contentH, outRect.height));
            Widgets.BeginScrollView(outRect, ref scroll, view);

            DrawHeader(new Rect(view.x, view.y, view.width, HeaderH), vis);
            if (twoButtonMode)
                DrawTwoButtonHeaderIcons(new Rect(view.x, view.y, view.width, HeaderH));

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
            float by = inRect.yMax - 32f;
            bool any = selected.Count > 0;
            var addBtn = new Rect(inRect.x, by, 220f, 30f);
            if (Widgets.ButtonText(addBtn, ASMKeys.AddSelected.Translate(selected.Count), active: any) && any)
            {
                if (twoButtonMode)
                    onPickedFlag?.Invoke(selected.Select(d => (d, selFlags[d])).ToList());
                else
                    onPicked?.Invoke(selected.ToList());
                Close();
            }
            if (Widgets.ButtonText(new Rect(addBtn.xMax + 8f, by, 180f, 30f), ASMKeys.ShowAllColumns.Translate()))
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
            GUI.DragWindow(new Rect(inRect.x, inRect.y, inRect.width, 12f));          // top pad (above table)
            GUI.DragWindow(new Rect(inRect.x, inRect.yMax - 40f, inRect.width, 40f)); // bottom bar (below table)
        }

        // In two-button mode, draw colored column indicators (green ✓, red ✗) in the checkbox area.
        private void DrawTwoButtonHeaderIcons(Rect r)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.green;
            Widgets.Label(new Rect(r.x, r.y, 22f, HeaderTopH), "✓");
            GUI.color = Color.red;
            Widgets.Label(new Rect(r.x + 22f, r.y, 22f, HeaderTopH), "✗");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
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
            float x = r.x + (twoButtonMode ? 44f : CheckW);
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
            x = r.x + (twoButtonMode ? 44f : CheckW);
            for (int i = 0; i < vis.Count; i++)
            {
                var col = vis[i];
                int colIdx = columns.IndexOf(col);
                bool isName = col.kind == ColKind.Name;
                Rect sub = new Rect(x, r.y + HeaderTopH, col.width, HeaderSubH);

                // Sort button.
                bool isSort = colIdx == sortIndex;
                Rect sortBtn = new Rect(sub.x, sub.y, 22f, HeaderSubH);
                TooltipHandler.TipRegion(sortBtn, ASMKeys.SortBy.Translate());
                if (Widgets.ButtonInvisible(sortBtn)) SetSort(colIdx);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = isSort ? Color.cyan : new Color(1f, 1f, 1f, 0.6f);
                Widgets.Label(sortBtn, isSort ? (sortAsc ? "▲" : "▼") : "↕");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                // Hide button (right). Name column is not hideable.
                float hideW = isName ? 0f : 20f;
                if (!isName)
                {
                    Rect hideBtn = new Rect(sub.xMax - hideW, sub.y + (HeaderSubH - 16f) / 2f, 16f, 16f);
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
                    Rect drag = new Rect(sortBtn.xMax, sub.y, Mathf.Max(sub.xMax - hideW - sortBtn.xMax, 8f), HeaderSubH);
                    ReorderableWidget.Reorderable(colGroup, drag);
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(drag, "↔");
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                }

                x += col.width;
            }

            // Gray gridlines between columns, between the name row and the control row, and under the header.
            GUI.color = Color.gray;
            x = r.x + (twoButtonMode ? 44f : CheckW);
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
                    float textW = Text.CalcSize(tc.col.header).x + 8f;
                    if (textW > tc.rect.width)
                    {
                        Rect overRect = new Rect(tc.rect.x, tc.rect.y, textW, tc.rect.height);
                        GUI.color = new Color(0.16f, 0.16f, 0.16f, 0.97f);
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

        private void DrawRow(Rect row, Row r, List<Col> vis)
        {
            if (twoButtonMode)
            {
                // Two paint zones: left = has (green), right = missing (red). Drag across to paint.
                Rect hasZone = new Rect(row.x, row.y, 22f, row.height);
                Rect missZone = new Rect(row.x + 22f, row.y, 22f, row.height);
                bool isHas = selected.Contains(r.def) && selFlags.ContainsKey(r.def) && selFlags[r.def];
                bool isMissing = selected.Contains(r.def) && selFlags.ContainsKey(r.def) && !selFlags[r.def];

                if (Mouse.IsOver(hasZone) || Mouse.IsOver(missZone))
                {
                    bool overHas = Mouse.IsOver(hasZone);
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        paintMode = true; paintValue = overHas;
                        selected.Add(r.def); selFlags[r.def] = overHas; Event.current.Use();
                    }
                    else if (Event.current.type == EventType.MouseDrag && paintMode)
                    {
                        if (overHas != paintValue) paintValue = overHas;
                        selected.Add(r.def); selFlags[r.def] = overHas;
                    }
                }

                // Vanilla CheckboxDraw — no color tint, native rendering.
                float cbY = row.y + (row.height - 18f) / 2f;
                Rect hasRect = new Rect(row.x + 2f, cbY, 18f, 18f);
                Rect missRect = new Rect(row.x + 22f, cbY, 18f, 18f);
                TooltipHandler.TipRegion(hasRect, ASMKeys.CondHasTip.Translate());
                TooltipHandler.TipRegion(missRect, ASMKeys.CondMissingTip.Translate());
                Widgets.CheckboxDraw(hasRect.x, hasRect.y, isHas, !isHas, 18f);
                Widgets.CheckboxDraw(missRect.x, missRect.y, isMissing, !isMissing, 18f);
            }
            else
            {
                // Paint selection: click + drag across the checkbox/name zone toggles rows together.
                Rect paintZone = new Rect(row.x, row.y, CheckW + NameW, row.height);
                bool value = selected.Contains(r.def);
                if (Mouse.IsOver(paintZone))
                {
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        paintMode = true;
                        paintValue = !value;
                        value = paintValue;
                        SetSel(r.def, value);
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.MouseDrag && paintMode)
                    {
                        if (value != paintValue) { value = paintValue; SetSel(r.def, value); }
                    }
                }

                // Draw-only checkbox: ALL input is handled by the paint logic above. Using Widgets.Checkbox
                // here would double-toggle a single click (its own click handler reverts the paint toggle).
                Rect cb = new Rect(row.x + 4f, row.y + (row.height - 24f) / 2f, 24f, 24f);
                Widgets.CheckboxDraw(cb.x, cb.y, value, false, 24f);
            }

            float x = row.x + (twoButtonMode ? 44f : CheckW);
            foreach (var col in vis)
            {
                Rect cell = new Rect(x, row.y, col.width, row.height);
                DrawCell(cell, r, col);
                x += col.width;
            }
        }

        private void SetSel(HediffDef d, bool on)
        {
            if (on) selected.Add(d); else selected.Remove(d);
        }

        private void ToggleSelFlag(HediffDef d, bool flag)
        {
            if (selected.Contains(d) && selFlags.ContainsKey(d) && selFlags[d] == flag)
            {
                selected.Remove(d);
                selFlags.Remove(d);
            }
            else
            {
                selected.Add(d);
                selFlags[d] = flag;
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
                    GUI.color = r.isBad ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(cell, (r.isBad ? ASMKeys.Bad : ASMKeys.Good).Translate());
                    GUI.color = Color.white;
                    break;
                case ColKind.Stat:
                    Text.Anchor = TextAnchor.MiddleCenter;
                    if (r.statStrings.TryGetValue(col.stat, out string sv))
                    {
                        GUI.color = r.statValues[col.stat] >= 0 ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
                        Widgets.Label(cell, sv);
                        GUI.color = Color.white;
                    }
                    break;
                case ColKind.Cap:
                    Text.Anchor = TextAnchor.MiddleCenter;
                    if (r.capStrings.TryGetValue(col.cap, out string cv))
                    {
                        GUI.color = r.capValues[col.cap] >= 0 ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
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
                case ColKind.Stat:
                    ordered = rows.OrderBy(r => r.statValues.TryGetValue(col.stat, out float v) ? v : 0f);
                    break;
                case ColKind.Cap:
                    ordered = rows.OrderBy(r => r.capValues.TryGetValue(col.cap, out float v) ? v : 0f);
                    break;
                default:
                    ordered = rows.OrderBy(r => r.def.LabelCap.ToString());
                    break;
            }
            return sortAsc ? ordered.ToList() : ordered.Reverse().ToList();
        }
    }
}
