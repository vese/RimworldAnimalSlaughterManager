using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM
{
    /// <summary>
    /// Unified preset manager for all three scopes:
    /// <list type="bullet">
    /// <item><description><c>All</c> — every kind (opened from the slaughter manager).</description></item>
    /// <item><description><c>Kind</c> — one kind's full settings (opened from the per-kind window).</description></item>
    /// <item><description><c>List</c> — a single trait list for one kind (opened from a section's preset button).</description></item>
    /// </list>
    /// Layout mirrors RimWorld's save/load list: a search row at the top (a magnifier + filter field
    /// spanning the left half, with a clear ✕ right after it, and scope-type toggles on the right half)
    /// and a save row pinned to the bottom; rows are zebra-striped with a top margin and side padding;
    /// a trash-icon delete button sits to the right of the load button, and same-scope rows also carry
    /// an overwrite button. The date is shown in a tiny font so it fits on one line. Lower-scope
    /// contexts also list higher-scope presets so they can be applied to the narrower target (only the
    /// target's slice is used); such presets are clearly marked in a dedicated column and cannot be
    /// deleted here (a preset is deletable only from a context at or above its own scope). Every row
    /// reserves the same set of columns, so columns stay aligned even when a row has no delete button,
    /// no overwrite button or no scope mark. Presets are sorted newest-first and can be filtered by
    /// name and by scope type. The save row is disabled when there is nothing to save or no name typed.
    /// </summary>
    public class Dialog_PresetBrowser : Window
    {
        private readonly ASM_MapComp comp;
        private readonly PresetScope scope;
        private readonly ThingDef kind;
        private readonly TraitListKind? listKind;

        private Vector2 scroll;
        private float listHeight = 9999f;
        private string nameBuffer = "";
        private string filterBuffer = "";
        private bool showAll = true, showKind = true, showList = true;

        private const float Gap = 6f;     // consistent vertical gap (title→filter→rows→rows)
        private const float PadX = 8f;    // row left/right padding
        private const float RowH = 30f;

        private int ContextRank => scope == PresetScope.All ? 3 : scope == PresetScope.Kind ? 2 : 1;
        private KindSettings Settings => comp.GetSettings(kind);
        private IList TargetList
        {
            get
            {
                var s = Settings;
                switch (listKind)
                {
                    case TraitListKind.Keep: return s.keepTraits;
                    case TraitListKind.Cull: return s.cullTraits;
                    case TraitListKind.Spare: return s.spareTraits;
                    default: return s.forceCullTraits;
                }
            }
        }

        public override Vector2 InitialSize => new Vector2(720f, 600f);

        public Dialog_PresetBrowser(ASM_MapComp comp, PresetScope scope, ThingDef kind, TraitListKind? listKind)
        {
            this.comp = comp;
            this.scope = scope;
            this.kind = kind;
            this.listKind = listKind;
            doCloseX = true;
            draggable = true;
            resizeable = true;
            optionalTitle = Title();
        }

        private string Title()
        {
            string kindLabel = kind != null ? kind.LabelCap.ToString() : "";
            switch (scope)
            {
                case PresetScope.Kind:
                    return "ASM.KindPresetsTitle".Translate(kindLabel);
                case PresetScope.List:
                    return "ASM.ListPresetsTitle".Translate(ListName(), kindLabel);
                default:
                    return "ASM.Presets".Translate();
            }
        }

        private string ListName()
        {
            switch (listKind)
            {
                case TraitListKind.Keep: return "ASM.ListNameKeep".Translate();
                case TraitListKind.Cull: return "ASM.ListNameCull".Translate();
                case TraitListKind.Spare: return "ASM.ListNameSpare".Translate();
                default: return "ASM.ListNameForceCull".Translate();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            float btnW = Mathf.Max(96f, Text.CalcSize("ASM.SavePreset".Translate()).x + 18f);

            // --- Top: search row, one Gap below the title. Magnifier + filter field spanning the left
            // half with a clear ✕ right after it; scope-type toggles fill the right half. ---
            float y = inRect.y + Gap;
            const float topH = 28f;
            float mid = inRect.x + inRect.width * 0.5f;
            const float icon = 22f, gap = 6f, clearGap = 10f;
            GUI.DrawTexture(new Rect(inRect.x, y + (topH - icon) / 2f, icon, icon), TexButton.Search);
            float fieldX = inRect.x + icon + gap;
            float fieldEnd = mid - icon - clearGap;
            filterBuffer = Widgets.TextField(new Rect(fieldX, y + 1f, fieldEnd - fieldX, 26f), filterBuffer);
            Rect clearBtn = new Rect(mid - icon, y + (topH - icon) / 2f, icon, icon);
            TooltipHandler.TipRegion(clearBtn, "ASM.Clear".Translate());
            if (Widgets.ButtonImage(clearBtn, TexButton.CloseXSmall))
                filterBuffer = "";
            // Scope-type filters (only the scopes visible in this context), right-aligned to the
            // window edge.
            var scopes = new List<PresetScope> { PresetScope.List, PresetScope.Kind, PresetScope.All };
            float toggleW = 0f;
            foreach (var sc in scopes)
                if (ScopeVisible(sc)) toggleW += ScopeToggleWidth(sc) + Gap;
            float tx = inRect.xMax - toggleW + Gap;
            foreach (var sc in scopes)
                DrawScopeToggle(ref tx, y, sc);
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
                    filterBuffer.Trim().NullOrEmpty() ? "ASM.NoPresets".Translate() : "ASM.NoPresetsFiltered".Translate());
                GUI.color = Color.white;
                cy += 28f;
            }
            int i = 0;
            foreach (var e in entries)
            {
                // Each row: top margin (Gap between rows) and left/right padding (PadX).
                Rect row = new Rect(view.x + PadX, cy, view.width - 2f * PadX, RowH);
                DrawEntryRow(row, e, i);
                cy += RowH + Gap;
                i++;
            }
            listHeight = Mathf.Max(cy - view.y, outRect.height);
            Widgets.EndScrollView();

            // Save row pinned to the bottom. The field is disabled when there is nothing to save; the
            // Save button is additionally disabled until a (non-empty) name is typed.
            bool hasData = HasSomethingToSave();
            bool canSave = hasData && !nameBuffer.Trim().NullOrEmpty();
            float sy = inRect.yMax - saveRowH + 2f;
            GUI.enabled = hasData;
            nameBuffer = Widgets.TextField(new Rect(inRect.x, sy, inRect.width - btnW - gap, 26f), nameBuffer);
            GUI.enabled = true;
            if (Widgets.ButtonText(new Rect(inRect.xMax - btnW, sy, btnW, 26f), "ASM.SavePreset".Translate(), active: canSave) && canSave)
                TrySave();
        }

        private static int ScopeRank(PresetScope s) => s == PresetScope.All ? 3 : s == PresetScope.Kind ? 2 : 1;

        private static float ScopeToggleWidth(PresetScope s)
        {
            string mark = s == PresetScope.All ? "ASM.PresetAllMark".Translate()
                        : s == PresetScope.Kind ? "ASM.PresetKindMark".Translate()
                        : "ASM.PresetListMark".Translate();
            const float iconS = 20f;
            return iconS + 4f + Text.CalcSize(mark).x + 8f;
        }
        // A scope is relevant in this context if it is the context's own scope or any broader one.
        private bool ScopeVisible(PresetScope s) => ScopeRank(s) >= ContextRank;
        private bool ScopeShown(PresetScope s) => s == PresetScope.All ? showAll : s == PresetScope.Kind ? showKind : showList;

        private void DrawScopeToggle(ref float tx, float y, PresetScope s)
        {
            if (!ScopeVisible(s)) return;
            bool on = ScopeShown(s);
            string mark = s == PresetScope.All ? "ASM.PresetAllMark".Translate()
                        : s == PresetScope.Kind ? "ASM.PresetKindMark".Translate()
                        : "ASM.PresetListMark".Translate();
            const float iconS = 20f;
            float labelW = Text.CalcSize(mark).x;
            float tw = iconS + 4f + labelW + 8f;
            Rect r = new Rect(tx, y + 1f, tw, 26f);
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
            Widgets.Label(new Rect(iconRect.xMax + 4f, r.y, labelW + 4f, r.height), mark);
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonInvisible(r))
            {
                if (s == PresetScope.All) showAll = !showAll;
                else if (s == PresetScope.Kind) showKind = !showKind;
                else showList = !showList;
            }
            tx += tw + Gap;
        }

        private List<PresetEntry> Gather()
        {
            var entries = new List<PresetEntry>();
            entries.AddRange(PresetIO.ListPresets(scope, listKind ?? TraitListKind.Keep));
            // Higher scopes are visible for cross-level application.
            if (scope == PresetScope.Kind) entries.AddRange(PresetIO.ListPresets(PresetScope.All));
            if (scope == PresetScope.List)
            {
                entries.AddRange(PresetIO.ListPresets(PresetScope.Kind));
                entries.AddRange(PresetIO.ListPresets(PresetScope.All));
            }
            entries = entries.OrderByDescending(e => e.date).ThenBy(e => e.name).ToList();

            string f = filterBuffer.Trim();
            if (!f.NullOrEmpty())
                entries = entries.Where(e => e.name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            // Scope-type filter (the toggles next to the search field).
            entries = entries.Where(e => ScopeShown(e.scope)).ToList();
            return entries;
        }

        private void DrawEntryRow(Rect row, PresetEntry e, int index)
        {
            bool deletable = e.DeletableFrom(ContextRank);
            bool crossLevel = e.scope != scope;
            string mark = crossLevel ? CrossLevelMark(e) : null;

            // Zebra rows, like the save/load list.
            if (index % 2 == 1) Widgets.DrawAltRect(row);

            // Columns are reserved right-to-left from the right edge; every row reserves the same set,
            // so columns stay aligned even when a row has no delete button, no overwrite button or no
            // scope mark. The "extra" slot holds the overwrite button (same-scope rows) or the scope
            // mark (cross-level rows) — they are mutually exclusive.
            float loadW = Mathf.Max(96f, Text.CalcSize("ASM.LoadPreset".Translate()).x + 18f);
            float extraW = Mathf.Max(120f, Text.CalcSize("ASM.OverwritePreset".Translate()).x + 18f);
            const float delW = 26f, dateW = 86f, gap = 6f;

            float right = row.xMax;
            Rect delRect = new Rect(right - delW, row.y + 2f, delW, 26f);
            right -= delW + gap;
            Rect loadBtn = new Rect(right - loadW, row.y + 2f, loadW, 26f);
            right -= loadW + gap;
            Rect dateRect = new Rect(right - dateW, row.y + 4f, dateW, 22f);
            right -= dateW + gap;
            Rect extraRect = new Rect(right - extraW, row.y + 2f, extraW, 26f);
            right -= extraW + gap;
            Rect nameRect = new Rect(row.x, row.y + 5f, Mathf.Max(right - row.x, 40f), 22f);

            var captured = e;

            // 1st column — name.
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(nameRect, e.name);
            Text.Anchor = TextAnchor.UpperLeft;

            // Extra column — overwrite (same scope) or scope mark (cross-level).
            if (!crossLevel)
            {
                if (Widgets.ButtonText(extraRect, "ASM.OverwritePreset".Translate()))
                {
                    var n = captured;
                    Find.WindowStack.Add(new Dialog_MessageBox("ASM.ConfirmOverwrite".Translate(n.name),
                        "ASM.OverwritePreset".Translate(), () => OverwriteEntry(n)));
                }
            }
            else if (mark != null)
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

            if (crossLevel) TooltipHandler.TipRegion(row, CrossLevelTip(e));

            if (Widgets.ButtonText(loadBtn, "ASM.LoadPreset".Translate()))
                ApplyEntry(captured);
            if (deletable)
            {
                TooltipHandler.TipRegion(delRect, "ASM.DeletePreset".Translate());
                if (Widgets.ButtonImage(delRect, TexButton.Delete))
                {
                    var n = captured;
                    string confirm = scope == PresetScope.List
                        ? "ASM.ConfirmDeleteList".Translate(n.name)
                        : scope == PresetScope.Kind ? "ASM.ConfirmDeleteKind".Translate(n.name) : "ASM.ConfirmDelete".Translate(n.name);
                    Find.WindowStack.Add(new Dialog_MessageBox(confirm, "ASM.DeletePreset".Translate(), () => PresetIO.Delete(n)));
                }
            }
        }

        // e.g. "[Все виды] " for an All-scope preset shown in a Kind/List context.
        private string CrossLevelMark(PresetEntry e)
        {
            if (e.scope == PresetScope.All) return "ASM.PresetAllMark".Translate();
            if (e.scope == PresetScope.Kind) return "ASM.PresetKindMark".Translate();
            return null;
        }

        private string CrossLevelTip(PresetEntry e)
        {
            string kindLabel = kind != null ? kind.LabelCap.ToString() : "";
            if (scope == PresetScope.Kind && e.scope == PresetScope.All)
                return "ASM.TipAllToKind".Translate(kindLabel);
            if (scope == PresetScope.List)
            {
                string ln = ListName();
                if (e.scope == PresetScope.All) return "ASM.TipAllToList".Translate(ln, kindLabel);
                if (e.scope == PresetScope.Kind) return "ASM.TipKindToList".Translate(ln, kindLabel);
            }
            return null;
        }

        // True when the current target actually has settings worth saving (so the save row is enabled).
        private bool HasSomethingToSave()
        {
            switch (scope)
            {
                case PresetScope.All: return comp.kindSettings.Values.Any(k => k != null && k.Customized);
                case PresetScope.Kind: return Settings.Customized;
                default: return TargetList.Count > 0;
            }
        }

        // Overwrite an existing same-scope preset with the current settings. Serialize overwrites a
        // same-name file, so this just reuses the normal export path under the preset's own name.
        private void OverwriteEntry(PresetEntry e)
        {
            switch (scope)
            {
                case PresetScope.All: PresetIO.ExportAll(e.name, comp); break;
                case PresetScope.Kind: PresetIO.ExportKind(e.name, kind, Settings); break;
                default: PresetIO.ExportList(e.name, listKind.Value, kind, TargetList); break;
            }
            Messages.Message("ASM.PresetOverwritten".Translate(e.name), MessageTypeDefOf.TaskCompletion, false);
        }

        private void TrySave()
        {
            string name = (nameBuffer ?? "").Trim();
            if (name.NullOrEmpty()) return;
            switch (scope)
            {
                case PresetScope.All: PresetIO.ExportAll(name, comp); break;
                case PresetScope.Kind: PresetIO.ExportKind(name, kind, Settings); break;
                default: PresetIO.ExportList(name, listKind.Value, kind, TargetList); break;
            }
            Messages.Message("ASM.PresetSaved".Translate(name), MessageTypeDefOf.TaskCompletion, false);
            nameBuffer = "";
        }

        private void ApplyEntry(PresetEntry e)
        {
            bool ok = true;
            switch (scope)
            {
                case PresetScope.All:
                    PresetIO.ApplyAll(e, comp);
                    break;
                case PresetScope.Kind:
                    ok = PresetIO.ApplyKindSettings(e, kind, Settings);
                    comp.MarkDirty();
                    break;
                default:
                    ok = PresetIO.ApplyList(e, listKind.Value, kind, TargetList);
                    comp.MarkDirty();
                    break;
            }
            if (ok)
                Messages.Message("ASM.PresetLoaded".Translate(e.name), MessageTypeDefOf.TaskCompletion, false);
            else
                Messages.Message("ASM.PresetNoSlice".Translate(kind.LabelCap), MessageTypeDefOf.RejectInput, false);
        }
    }
}
