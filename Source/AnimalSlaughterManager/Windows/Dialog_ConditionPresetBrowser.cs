using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ASM.Comps;
using ASM.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM.Windows
{
    /// <summary>
    /// Preset browser for a single condition-list bucket. Mirrors Dialog_PresetBrowser's layout
    /// (search + scope toggles + save row, zebra list with load/overwrite/delete).
    /// </summary>
    public class Dialog_ConditionPresetBrowser : Window
    {
        private readonly ASM_MapComp comp;
        private readonly CondBucket bucket;
        private readonly List<SlaughterCondition> targetList;

        private Vector2 scroll;
        private float listHeight = 9999f;
        private string nameBuffer = "";
        private string filterBuffer = "";
        private bool showCond = true, showKind = true, showAll = true;

        private const float Gap = 6f;
        private const float PadX = 8f;
        private const float RowH = 30f;

        public override Vector2 InitialSize => new Vector2(820f, 600f);

        public Dialog_ConditionPresetBrowser(ASM_MapComp comp, CondBucket bucket, List<SlaughterCondition> targetList)
        {
            this.comp = comp;
            this.bucket = bucket;
            this.targetList = targetList;
            doCloseX = true;
            draggable = true;
            resizeable = true;
            optionalTitle = ASMKeys.CondPresetTitle.Translate(BucketLabel(bucket));
        }

        private bool HasData => targetList != null && targetList.Count > 0;

        private static string BucketLabel(CondBucket b)
        {
            switch (b)
            {
                case CondBucket.AdultMale: return ASMKeys.AdultMales.Translate();
                case CondBucket.YoungMale: return ASMKeys.YoungMales.Translate();
                case CondBucket.AdultFemale: return ASMKeys.AdultFemales.Translate();
                default: return ASMKeys.YoungFemales.Translate();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            float btnW = Mathf.Max(96f, Text.CalcSize(ASMKeys.SavePreset.Translate()).x + 18f);

            // --- Top: search row + scope toggles, right-aligned ---
            float y = inRect.y + Gap;
            const float topH = 28f;
            const float icon = 22f, clearGap = 10f;

            // Scope toggles (right-aligned), like Dialog_PresetBrowser.
            float toggleX = inRect.xMax;
            float twCond = ScopeToggleWidth(PresetScope.List);
            float twKind = ScopeToggleWidth(PresetScope.Kind);
            float twAll = ScopeToggleWidth(PresetScope.All);
            toggleX -= twAll; DrawScopeToggle(toggleX, y, twAll, PresetScope.All, ref showAll);
            toggleX -= twKind + Gap; DrawScopeToggle(toggleX, y, twKind, PresetScope.Kind, ref showKind);
            toggleX -= twCond + Gap; DrawScopeToggle(toggleX, y, twCond, PresetScope.List, ref showCond);

            // Filter field (left half up to toggles).
            GUI.DrawTexture(new Rect(inRect.x, y + (topH - icon) / 2f, icon, icon), TexButton.Search);
            float fieldX = inRect.x + icon + Gap;
            float fieldEnd = toggleX - icon - clearGap;
            filterBuffer = Widgets.TextField(new Rect(fieldX, y + 1f, fieldEnd - fieldX, 26f), filterBuffer);
            Rect clearBtn = new Rect(toggleX - icon - clearGap + clearGap, y + (topH - icon) / 2f, icon, icon);
            TooltipHandler.TipRegion(clearBtn, ASMKeys.Clear.Translate());
            if (Widgets.ButtonImage(clearBtn, TexButton.CloseXSmall))
                filterBuffer = "";
            y += topH + Gap;

            // --- Bottom: save row ---
            const float saveRowH = 30f;
            float listBottom = inRect.yMax - saveRowH - Gap;

            // --- Middle: list ---
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

            // Save row: name field + Save.
            float sy = inRect.yMax - saveRowH + 2f;
            bool canSave = HasData && !nameBuffer.Trim().NullOrEmpty();
            GUI.enabled = HasData;
            nameBuffer = Widgets.TextField(new Rect(inRect.x, sy, inRect.width - btnW - Gap, 26f), nameBuffer);
            if (Widgets.ButtonText(new Rect(inRect.xMax - btnW, sy, btnW, 26f), ASMKeys.SavePreset.Translate(), active: canSave) && canSave)
                TrySave();
            GUI.enabled = true;
        }

        private static float ScopeToggleWidth(PresetScope s)
        {
            string mark = s == PresetScope.All ? ASMKeys.PresetAllMark.Translate()
                        : s == PresetScope.Kind ? ASMKeys.PresetKindMark.Translate()
                        : ASMKeys.PresetListMark.Translate();
            return 20f + 4f + Text.CalcSize(mark).x + 8f;
        }

        private static void DrawScopeToggle(float x, float y, float w, PresetScope s, ref bool on)
        {
            string mark = s == PresetScope.All ? ASMKeys.PresetAllMark.Translate()
                        : s == PresetScope.Kind ? ASMKeys.PresetKindMark.Translate()
                        : ASMKeys.PresetListMark.Translate();
            Rect r = new Rect(x, y + 1f, w, 26f);
            Widgets.DrawHighlightIfMouseover(r);
            Rect iconRect = new Rect(r.x, r.y + (r.height - 20f) / 2f, 20f, 20f);
            if (on)
            {
                GUI.color = Color.green;
                Widgets.CheckboxDraw(iconRect.x, iconRect.y, true, false, 20f);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.red;
                GUI.DrawTexture(iconRect, TexButton.CloseXSmall);
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(iconRect.xMax + 4f, r.y, Text.CalcSize(mark).x + 4f, r.height), mark);
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonInvisible(r)) on = !on;
        }

        private List<PresetEntry> Gather()
        {
            var entries = new List<PresetEntry>();
            if (showCond) entries.AddRange(PresetIO.ListConditionPresets(bucket));
            if (showKind) entries.AddRange(PresetIO.ListPresets(PresetScope.Kind));
            if (showAll) entries.AddRange(PresetIO.ListPresets(PresetScope.All));
            entries = entries.OrderByDescending(e => e.date).ThenBy(e => e.name).ToList();
            string f = filterBuffer.Trim();
            if (!f.NullOrEmpty())
                entries = entries.Where(e => e.name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            return entries;
        }

        private void DrawEntryRow(Rect row, PresetEntry e, int index)
        {
            bool isCondPreset = e.scope == PresetScope.List && e.condBucket != null;
            bool sameBucket = e.condBucket == bucket;
            bool isKindOrAll = e.scope == PresetScope.Kind || e.scope == PresetScope.All;
            bool canOverwrite = isCondPreset && sameBucket && HasData;
            if (index % 2 == 1) Widgets.DrawAltRect(row);
            float loadW = Mathf.Max(96f, Text.CalcSize(ASMKeys.LoadPreset.Translate()).x + 18f);
            float overW = Mathf.Max(96f, Text.CalcSize(ASMKeys.OverwritePreset.Translate()).x + 18f);
            const float delW = 26f, dateW = 86f, gap = 6f;
            // Mark width: wide enough for the longest bucket label (e.g. "Молодые самки").
            Text.Font = GameFont.Tiny;
            float markW = Mathf.Max(80f, Mathf.Max(
                Text.CalcSize(BucketLabel(CondBucket.AdultMale)).x,
                Text.CalcSize(BucketLabel(CondBucket.YoungFemale)).x) + 8f);
            Text.Font = GameFont.Small;

            // Columns right-to-left: delete, load, overwrite (left of load), date, mark, name.
            float right = row.xMax;
            Rect delRect = new Rect(right - delW, row.y + 2f, delW, 26f); right -= delW + gap;
            Rect loadBtn = new Rect(right - loadW, row.y + 2f, loadW, 26f); right -= loadW + gap;
            Rect overRect = new Rect(right - overW, row.y + 2f, overW, 26f); right -= overW + gap;
            Rect dateRect = new Rect(right - dateW, row.y + 4f, dateW, 22f); right -= dateW + gap;
            Rect markRect = new Rect(right - markW, row.y + 4f, markW, 22f); right -= markW + gap;
            Rect nameRect = new Rect(row.x, row.y + 5f, Mathf.Max(right - row.x, 40f), 22f);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(nameRect, e.name);
            Text.Anchor = TextAnchor.UpperLeft;

            // Mark column: scope mark for cross-scope/cross-bucket (reserved even if empty).
            string mark = null;
            if (isKindOrAll) mark = e.scope == PresetScope.All ? ASMKeys.PresetAllMark.Translate() : ASMKeys.PresetKindMark.Translate();
            else if (isCondPreset && !sameBucket) mark = BucketLabel(e.condBucket.Value);
            if (mark != null)
            {
                GUI.color = Color.cyan;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(markRect, mark);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(dateRect, e.date.ToString("yyyy-MM-dd HH:mm"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Overwrite button (left of Load, same-bucket + has data only; space always reserved).
            if (canOverwrite)
            {
                var captured0 = e;
                if (Widgets.ButtonText(overRect, ASMKeys.OverwritePreset.Translate()))
                {
                    var n = captured0;
                    Find.WindowStack.Add(new Dialog_MessageBox(ASMKeys.ConfirmOverwrite.Translate(n.name),
                        ASMKeys.OverwritePreset.Translate(), () => OverwriteEntry(n),
                        "No".Translate()) { doCloseX = true });
                }
            }

            if (isKindOrAll) TooltipHandler.TipRegion(row, ASMKeys.CondPresetCrossBucket.Translate(e.scope.ToString()));

            var captured = e;
            if (Widgets.ButtonText(loadBtn, ASMKeys.LoadPreset.Translate()))
                ApplyEntry(captured);
            TooltipHandler.TipRegion(delRect, ASMKeys.DeletePreset.Translate());
            if (Widgets.ButtonImage(delRect, TexButton.Delete))
            {
                var n = captured;
                Find.WindowStack.Add(new Dialog_MessageBox(ASMKeys.ConfirmDelete.Translate(n.name),
                    ASMKeys.DeletePreset.Translate(), () => PresetIO.Delete(n),
                    "No".Translate()) { doCloseX = true });
            }
        }

        private void TrySave()
        {
            string name = (nameBuffer ?? "").Trim();
            if (name.NullOrEmpty()) return;
            PresetIO.ExportConditions(name, bucket, targetList);
            Messages.Message(ASMKeys.PresetSaved.Translate(name), MessageTypeDefOf.TaskCompletion, false);
            nameBuffer = "";
        }

        private void OverwriteByName(string name)
        {
            PresetIO.ExportConditions(name, bucket, targetList);
            Messages.Message(ASMKeys.PresetOverwritten.Translate(name), MessageTypeDefOf.TaskCompletion, false);
        }

        private void OverwriteEntry(PresetEntry e)
        {
            PresetIO.ExportConditions(e.name, bucket, targetList);
            Messages.Message(ASMKeys.PresetOverwritten.Translate(e.name), MessageTypeDefOf.TaskCompletion, false);
        }

        private void ApplyEntry(PresetEntry e)
        {
            if (e.scope == PresetScope.List && e.condBucket != null)
            {
                var loaded = PresetIO.ApplyConditions(e);
                if (loaded != null)
                {
                    targetList.Clear();
                    targetList.AddRange(loaded);
                    comp.MarkDirty();
                    Messages.Message(ASMKeys.PresetLoaded.Translate(e.name), MessageTypeDefOf.TaskCompletion, false);
                }
            }
            else
            {
                // Kind or All preset — extract the matching bucket.
                try
                {
                    var ser = new XmlSerializer(typeof(SlaughterPresetDto));
                    using (var r = new StreamReader(e.path))
                    {
                        var dto = (SlaughterPresetDto)ser.Deserialize(r);
                        KindDto kd = dto.Kinds.FirstOrDefault();
                        if (kd == null) { Messages.Message(ASMKeys.PresetNoSlice.Translate(""), MessageTypeDefOf.RejectInput, false); return; }
                        List<ConditionDto> source = bucket == CondBucket.AdultMale ? kd.PrioAdultMale
                            : bucket == CondBucket.YoungMale ? kd.PrioYoungMale
                            : bucket == CondBucket.AdultFemale ? kd.PrioAdultFemale : kd.PrioYoungFemale;
                        if (source == null || source.Count == 0) { Messages.Message(ASMKeys.PresetNoSlice.Translate(""), MessageTypeDefOf.RejectInput, false); return; }
                        targetList.Clear();
                        foreach (var cd in source) targetList.Add(cd.ToCondition());
                        comp.MarkDirty();
                        Messages.Message(ASMKeys.PresetLoaded.Translate(e.name), MessageTypeDefOf.TaskCompletion, false);
                    }
                }
                catch { }
            }
        }
    }
}
