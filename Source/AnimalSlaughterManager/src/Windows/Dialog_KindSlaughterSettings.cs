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
    /// Per-animal-kind slaughter settings, in two tabs:
    /// • Priorities — sex×age (older/younger) buckets and cull/spare trait priorities.
    /// • Special rules — breeding ("keep") protection traits and force-slaughter traits
    ///   (slaughter regardless of count/limits; protection still wins).
    ///
    /// The header carries the animal icon/name plus the per-kind preset button and the two reset
    /// buttons (reset this tab / reset all tabs) in one row, to the right of the name. Each trait
    /// section has, to the right of its title, a clear-list button (empties the whole list), the
    /// add-trait button, and a single list-preset button (which both saves and loads — one window
    /// covers export and import). Trait rows have a drag handle, a copy button, and
    /// age/gender/inheritable columns, so a list configured for one kind can be saved and loaded
    /// into another. The trait lists expand to fill the window's remaining height (resizing with the
    /// window), with a 150px minimum; below that the whole window scrolls. Blocks (preferences, each
    /// trait section) are separated by divider lines. The window is not draggable as a whole (that
    /// made row reorder move the window); it drags from its frame and the header/tab band instead.
    /// </summary>
    public class Dialog_KindSlaughterSettings : Window
    {
        private readonly ASM_MapComp comp;
        private readonly ThingDef animalDef;
        private readonly KindSettings settings;
        private Vector2 topScroll;
        private Vector2 keepScroll, forceCullScroll;
        private float keepListHeight = 200f, forceCullListHeight = 120f;
        private int keepGroup = -1, forceCullGroup = -1;
        private Vector2 condScroll1, condScroll2, condScroll3, condScroll4;
        private float condListH1 = 80f, condListH2 = 80f, condListH3 = 80f, condListH4 = 80f;
        private int condGroup1 = -1, condGroup2 = -1, condGroup3 = -1, condGroup4 = -1;
        private static List<SlaughterCondition> clipboard;
        private int currentTab; // 0 = Priorities, 1 = Special rules

        // Unified column x-offsets (relative to row.x), shared by all four row kinds so columns
        // line up within a tab. Keep rows additionally fill the keep-count column.
        private const float GripX = 0f, GripW = 22f;
        private const float TraitX = 24f;
        private const float KeepColX = 286f, KeepColW = 86f;   // keep-count (keep rows only)
        private const float AgeX = 372f, AgeW = 104f;
        private const float GenderX = 478f, GenderW = 96f;
        private const float InhX = 576f, InhW = 144f;
        private const float CopyIconS = 20f;
        private static float TraitWidth(bool isKeep) => isKeep ? (KeepColX - TraitX) : (AgeX - TraitX);

        private const float TabBarH = 32f;
        private const float MinListH = 150f;
        // Per-section fixed chrome: section label (30) + help (22) + column headers (20). The add-trait
        // button lives in the section header row, so it adds no height here.
        private const float SectionNonListH = 30f + 22f + 20f;
        private const float HeaderH = 34f;
        private const float TabGap = 4f;                  // gap below the tab strip into the tab content
        private const float TabStripH = TabBarH + TabGap; // reserved tab band (32) + gap into content
        private const float PrefsH = 4f * 30f + 8f;
        private const float CondSectionH = 60f;   // title row (30) + button row (30)
        private const float DividerGap = 8f;   // vertical room taken by a block divider line

        // Everything except the two list bodies, for the active tab. Matches the drawn flow exactly so
        // the two lists fill the window precisely (header + divider + tab bar + tab chrome).
        private float NonListHeight()
        {
            float h = HeaderH + TabStripH;
            if (currentTab == 0)
                h += PrefsH + DividerGap + 2 * CondSectionH + DividerGap;  // prefs, 2 rows of section titles (2×2 grid), inter-row divider
            else if (currentTab == 1)
                h += SectionNonListH + DividerGap + SectionNonListH;           // keep, divider, forceCull
            else
                h += PrefsH + 8f + 40f;                                        // General: 4 pref rows + help
            return h;
        }
        private float MinContentHeight()
        {
            if (currentTab == 2) return NonListHeight();
            int n = currentTab == 0 ? 4 : 2;
            float minLH = currentTab == 0 ? 60f : MinListH;
            return NonListHeight() + n * minLH;
        }

        public override Vector2 InitialSize
        {
            get
            {
                // Open at the larger tab's minimum (lists = 150); the user can resize taller to grow lists.
                float minContent = Mathf.Max(
                    HeaderH + TabStripH + PrefsH + DividerGap + 4 * CondSectionH + 3 * DividerGap + 4 * 60f,
                    HeaderH + TabStripH + SectionNonListH + DividerGap + SectionNonListH + 2f * MinListH);
                float maxH = Screen.height * 0.95f;
                return new Vector2(980f, Mathf.Min(Screen.height * 0.85f, maxH));
            }
        }

        public Dialog_KindSlaughterSettings(ASM_MapComp comp, ThingDef animalDef)
        {
            this.comp = comp;
            this.animalDef = animalDef;
            settings = comp.GetSettings(animalDef);
            doCloseX = true;
            // Not draggable as a whole: a draggable window ends OnGUI with GUI.DragWindow() (no args),
            // which grabs any unclaimed press — so reordering a row in the shorter/empty second list
            // (spare/forceCull) moved the window instead. The window drags from its frame + the
            // header/tab band (LateWindowOnGUI); the lists never start a window drag, so all four
            // lists reorder cleanly.
            draggable = false;
            resizeable = true;
        }

        public override void PreClose()
        {
            base.PreClose();
            var msg = ValidateAllConditions();
            if (msg != null)
            {
                Messages.Message(msg, MessageTypeDefOf.NegativeHealthEvent, false);
                Find.WindowStack.Add(new Dialog_MessageBox(msg));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            int numLists = currentTab == 0 ? 2 : 2;
            float minLH = currentTab == 0 ? 60f : MinListH;
            float minContent = MinContentHeight();
            if (inRect.height >= minContent)
            {
                // Lists expand to fill the window.
                float perList = (inRect.height - NonListHeight()) / numLists;
                DrawContent(inRect.x, inRect.y, inRect.width, perList);
            }
            else
            {
                // Window shrunk below minimum: keep lists at min and scroll the whole content.
                float perList = minLH;
                float vw = inRect.width - 16f;
                Rect view = new Rect(inRect.x, inRect.y, vw, minContent);
                Widgets.BeginScrollView(inRect, ref topScroll, view);
                DrawContent(view.x, view.y, vw, perList);
                Widgets.EndScrollView();
            }
        }

        // The window is not draggable as a whole (see ctor). Drag instead from the frame on all four
        // sides plus the header/tab band — never from the trait lists, so row reorder is never stolen.
        protected override void LateWindowOnGUI(Rect inRect)
        {
            float m = inRect.x;
            float winW = inRect.xMax + m;
            float winH = inRect.yMax + m;
            GUI.DragWindow(new Rect(0, 0, winW, m));                                    // top frame
            GUI.DragWindow(new Rect(0, winH - m, winW, m));                             // bottom frame
            GUI.DragWindow(new Rect(0, 0, m, winH));                                    // left frame
            GUI.DragWindow(new Rect(winW - m, 0, m, winH));                             // right frame
            GUI.DragWindow(new Rect(inRect.x, inRect.y, inRect.width, HeaderH + TabBarH)); // header + tab strip
        }

        private void DrawContent(float x, float y, float w, float listH)
        {
            // Header: animal icon + name on the left; the per-kind preset button and the two reset
            // buttons sit to the right of the name. On the General tab they are hidden.
            Widgets.ThingIcon(new Rect(x, y, 28f, 28f), animalDef);
            const float btnH = 26f, gap = 6f;
            float right = x + w - 24f; // keep clear of the window close-X in the top-right corner
            bool showHeaderButtons = currentTab != 2; // hidden on General tab
            string resetTabKey = currentTab == 0 ? "ASM.ResetPriorities" : "ASM.ResetSpecialRules";
            float presetW = Mathf.Max(120f, Text.CalcSize("ASM.KindPresets".Translate()).x + 18f);
            float resetTabW = Mathf.Max(120f, Text.CalcSize(resetTabKey.Translate()).x + 18f);
            float resetAllW = Mathf.Max(140f, Text.CalcSize("ASM.ResetKind".Translate()).x + 18f);
            float labelRight = showHeaderButtons ? right - resetAllW - resetTabW - presetW - 3f * gap : right;
            Rect resetAllBtn = new Rect(right - resetAllW, y + 1f, resetAllW, btnH);
            right -= resetAllW + gap;
            Rect resetTabBtn = new Rect(right - resetTabW, y + 1f, resetTabW, btnH);
            right -= resetTabW + gap;
            Rect presetBtn = new Rect(right - presetW, y + 1f, presetW, btnH);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x + 34f, y, Mathf.Max(labelRight - (x + 34f), 40f), 28f), animalDef.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (showHeaderButtons)
            {
                if (Widgets.ButtonText(presetBtn, "ASM.KindPresets".Translate()))
                    Find.WindowStack.Add(new Dialog_PresetBrowser(comp, PresetScope.Kind, animalDef, null));
                if (Widgets.ButtonText(resetTabBtn, resetTabKey.Translate()))
                    ResetCurrentTab();
                if (Widgets.ButtonText(resetAllBtn, "ASM.ResetKind".Translate()))
                {
                    settings.Reset();
                    settings.malePref = comp.globalMalePref;
                    settings.femalePref = comp.globalFemalePref;
                    settings.maleYoungPref = comp.globalMaleYoungPref;
                    settings.femaleYoungPref = comp.globalFemaleYoungPref;
                    comp.MarkDirty();
                }
            }
            y += HeaderH;

            // Tab strip. TabDrawer draws the tab buttons in the 32px band ABOVE the base rect (they
            // hang from the rect's top edge upward), so reserve that band here and put the base rect
            // at its bottom edge — otherwise the tabs render upward into the kind-name header.
            y += TabBarH;
            TabDrawer.DrawTabs(new Rect(x, y, w, TabBarH), new List<TabRecord>
            {
                new TabRecord("ASM.TabPriorities".Translate(), () => currentTab = 0, currentTab == 0),
                new TabRecord("ASM.TabExceptions".Translate(), () => currentTab = 1, currentTab == 1),
                new TabRecord("ASM.TabGeneral".Translate(), () => currentTab = 2, currentTab == 2),
            });
            y += TabGap;

            if (currentTab == 0) DrawPrioritiesTab(x, y, w, listH);
            else if (currentTab == 1) DrawExceptionsTab(x, y, w, listH);
            else DrawGeneralTab(x, y, w, listH);
        }

        // A faint horizontal line separating blocks (header / preferences / trait sections).
        private static float DrawBlockDivider(float x, float y, float w)
        {
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.25f);
            Widgets.DrawLineHorizontal(x, y + DividerGap / 2f, w);
            GUI.color = prev;
            return y + DividerGap;
        }

        private void ResetCurrentTab()
        {
            if (currentTab == 0)
            {
                settings.malePref = comp.globalMalePref;
                settings.femalePref = comp.globalFemalePref;
                settings.maleYoungPref = comp.globalMaleYoungPref;
                settings.femaleYoungPref = comp.globalFemaleYoungPref;
                settings.cullTraits.Clear();
                settings.spareTraits.Clear();
                settings.prioAdultMale.Clear();
                settings.prioYoungMale.Clear();
                settings.prioAdultFemale.Clear();
                settings.prioYoungFemale.Clear();
            }
            else if (currentTab == 1)
            {
                settings.keepTraits.Clear();
                settings.forceCullTraits.Clear();
            }
            else
            {
                settings.malePref = comp.globalMalePref;
                settings.femalePref = comp.globalFemalePref;
                settings.maleYoungPref = comp.globalMaleYoungPref;
                settings.femaleYoungPref = comp.globalFemaleYoungPref;
            }
            comp.MarkDirty();
        }

        // A thicker, more distinct separator line (for separating the help text block from
        // the age-preference rows above and the condition sections below).
        private static float DrawSectionSeparator(float x, float y, float w)
        {
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            Widgets.DrawLineHorizontal(x, y + DividerGap / 2f, w);
            Widgets.DrawLineHorizontal(x, y + DividerGap / 2f + 2f, w);
            GUI.color = prev;
            return y + DividerGap;
        }

        private void DrawPrioritiesTab(float x, float y, float w, float listH)
        {
            y = DrawPrefRow(x, y, w, "ASM.AdultMales", true, true);
            y = DrawPrefRow(x, y, w, "ASM.YoungMales", true, false);
            y = DrawPrefRow(x, y, w, "ASM.AdultFemales", false, true);
            y = DrawPrefRow(x, y, w, "ASM.YoungFemales", false, false);
            y += 8f;

            // Distinct double-line separator above the help text (thicker than section dividers).
            y = DrawSectionSeparator(x, y, w);

            // Gray help text — same style as other tabs.
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(x, y, w, 36f), "ASM.PriorityHelp".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += 36f;

            // 2×2 grid with dividers between all sections + vertical divider between columns.
            float halfW = (w - 12f) / 2f;
            float x2 = x + halfW + 12f;
            DrawConditionSection(x, y, halfW, listH, "ASM.AdultMales", settings.prioAdultMale, ref condScroll1, ref condListH1, ref condGroup1, true, true);
            // Vertical divider between columns.
            Color vdPrev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.25f);
            Widgets.DrawLineVertical(x + halfW + 6f, y, listH + CondSectionH);
            GUI.color = vdPrev;
            DrawConditionSection(x2, y, halfW, listH, "ASM.YoungMales", settings.prioYoungMale, ref condScroll2, ref condListH2, ref condGroup2, true, false);
            y += CondSectionH + listH;
            y = DrawBlockDivider(x, y, w);
            DrawConditionSection(x, y, halfW, listH, "ASM.AdultFemales", settings.prioAdultFemale, ref condScroll3, ref condListH3, ref condGroup3, false, true);
            DrawConditionSection(x2, y, halfW, listH, "ASM.YoungFemales", settings.prioYoungFemale, ref condScroll4, ref condListH4, ref condGroup4, false, false);
        }

        // ---- General tab: 4 age×sex prefs + cross-kind propagation ----

        private void DrawGeneralTab(float x, float y, float w, float listH)
        {
            y = DrawGeneralPrefRow(x, y, w, "ASM.AdultMales", true, true);
            y = DrawGeneralPrefRow(x, y, w, "ASM.YoungMales", true, false);
            y = DrawGeneralPrefRow(x, y, w, "ASM.AdultFemales", false, true);
            y = DrawGeneralPrefRow(x, y, w, "ASM.YoungFemales", false, false);
            y += 8f;
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(x, y, w, 40f), "ASM.GeneralTabHelp".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private float DrawGeneralPrefRow(float x, float y, float w, string labelKey, bool male, bool adult)
        {
            Rect row = new Rect(x, y, w, 30f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x, row.y, 180f, row.height), labelKey.Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            float bw = 200f;
            var cur = comp.GetGlobalPref(male, adult);
            if (ChoiceButton(new Rect(row.x + 190f, row.y + 3f, bw, row.height - 6f), "ASM.OldestFirst".Translate(), cur == SlaughterPreference.OldestFirst))
                SetGeneralPref(male, adult, SlaughterPreference.OldestFirst);
            if (ChoiceButton(new Rect(row.x + 190f + bw + 8f, row.y + 3f, bw, row.height - 6f), "ASM.YoungestFirst".Translate(), cur == SlaughterPreference.YoungestFirst))
                SetGeneralPref(male, adult, SlaughterPreference.YoungestFirst);
            return row.yMax;
        }

        private void SetGeneralPref(bool male, bool adult, SlaughterPreference pref)
        {
            if (comp.GetGlobalPref(male, adult) == pref) return;
            // General tab sets the global default AND propagates to all kinds.
            if (male) { if (adult) comp.globalMalePref = pref; else comp.globalMaleYoungPref = pref; }
            else { if (adult) comp.globalFemalePref = pref; else comp.globalFemaleYoungPref = pref; }
            foreach (var kv in comp.kindSettings)
                if (kv.Value != null)
                    kv.Value.SetPref(male, adult, pref);
            comp.MarkDirty();
        }

        // ---- Validation ----

        // Returns true if a condition conflicts with or duplicates another in the same list.
        // Returns true if a condition conflicts with or duplicates another in the same list.
        // Training states: 2 of 3 is fine; all 3 or exact duplicates are conflicts.
        public static bool IsConditionProblematic(List<SlaughterCondition> list, int index)
        {
            var c = list[index];
            bool isTrainingState = c.type == CondType.TrainingNone || c.type == CondType.TrainingPartial || c.type == CondType.TrainingFull;

            if (!isTrainingState)
            {
                string key = ConditionKey(c);
                for (int i = 0; i < list.Count; i++)
                {
                    if (i == index) continue;
                    if (ConditionKey(list[i]) != key) continue;
                    return true;
                }
                return false;
            }

            // Training states: exact duplicate = conflict; all 3 present = conflict; 2 of 3 = fine.
            int otherTraining = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (i == index) continue;
                var ot = list[i].type;
                if (ot == CondType.TrainingNone || ot == CondType.TrainingPartial || ot == CondType.TrainingFull)
                {
                    if (ot == c.type) return true;        // exact duplicate
                    otherTraining++;
                }
            }
            return otherTraining >= 2;  // this + 2 others = all 3 = conflict
        }

        // Returns a comma-separated list of conflicting condition labels, or null if none found.
        public static string FindConflicts(List<SlaughterCondition> list, int index)
        {
            var c = list[index];
            var conflicts = new List<string>();
            bool isTrainingState = c.type == CondType.TrainingNone || c.type == CondType.TrainingPartial || c.type == CondType.TrainingFull;

            if (!isTrainingState)
            {
                string key = ConditionKey(c);
                for (int i = 0; i < list.Count; i++)
                {
                    if (i == index) continue;
                    if (ConditionKey(list[i]) == key)
                        conflicts.Add(list[i].Label);
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (i == index) continue;
                    var ot = list[i].type;
                    if (ot == CondType.TrainingNone || ot == CondType.TrainingPartial || ot == CondType.TrainingFull)
                        if (!conflicts.Contains(list[i].Label))
                            conflicts.Add(list[i].Label);
                }
            }
            return conflicts.Count > 0 ? string.Join(", ", conflicts.ToArray()) : null;
        }

        // A string key identifying what a condition tests (ignoring has). Same key = same test subject.
        private static string ConditionKey(SlaughterCondition c)
        {
            switch (c.type)
            {
                case CondType.Trait: return "Trait:" + (c.trait?.defName ?? "") + ":" + c.inheritMode;
                case CondType.Disease: return "Disease:" + (c.disease?.defName ?? "");
                case CondType.Training: return "Training:" + (c.trainable?.defName ?? "");
                case CondType.TrainingNone:
                case CondType.TrainingPartial:
                case CondType.TrainingFull:
                    return "TrainingState";
                case CondType.HasPositiveTrait: return "PositiveTrait";
                case CondType.HasNegativeTrait: return "NegativeTrait";
                default: return c.type.ToString();
            }
        }

        // Check all four condition lists for problems. Returns a human-readable summary, or null if OK.
        private string ValidateAllConditions()
        {
            var problems = new List<string>();
            void CheckList(string name, List<SlaughterCondition> list)
            {
                int issues = 0;
                for (int i = 0; i < list.Count; i++)
                    if (IsConditionProblematic(list, i)) issues++;
                if (issues > 0)
                    problems.Add(name + " (" + issues + ")");
            }
            CheckList("ASM.AdultMales".Translate(), settings.prioAdultMale);
            CheckList("ASM.YoungMales".Translate(), settings.prioYoungMale);
            CheckList("ASM.AdultFemales".Translate(), settings.prioAdultFemale);
            CheckList("ASM.YoungFemales".Translate(), settings.prioYoungFemale);
            if (problems.Count == 0) return null;
            return "ASM.ValidationProblems".Translate(animalDef.LabelCap, string.Join(", ", problems));
        }

        // Condition list preset menu: save, load (replace), import from kind/all.
        private CondBucket BucketOf(bool male, bool adult) =>
            male ? (adult ? CondBucket.AdultMale : CondBucket.YoungMale)
                : (adult ? CondBucket.AdultFemale : CondBucket.YoungFemale);

        private bool HasClipboard => clipboard != null && clipboard.Count > 0;

        private void InheritDropdown(Rect rect, SlaughterCondition cond)
        {
            string label = cond.inheritMode == InheritableFilter.Inheritable ? "ASM.InhInheritable".Translate()
                         : cond.inheritMode == InheritableFilter.NonInheritable ? "ASM.InhNonInheritable".Translate()
                         : "ASM.InhBoth".Translate();
            if (Widgets.ButtonText(rect, label))
            {
                var opts = new List<FloatMenuOption>();
                foreach (InheritableFilter s in (InheritableFilter[])Enum.GetValues(typeof(InheritableFilter)))
                { var c = s; opts.Add(new FloatMenuOption(InhLabel(s), () => { cond.inheritMode = c; comp.MarkDirty(); })); }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
        }

        private static string InhLabel(InheritableFilter s)
        {
            switch (s) { case InheritableFilter.Inheritable: return "ASM.InhInheritable".Translate(); case InheritableFilter.NonInheritable: return "ASM.InhNonInheritable".Translate(); default: return "ASM.InhBoth".Translate(); }
        }

        private void OpenConditionPresetMenu(List<SlaughterCondition> list, bool male, bool adult)
        {
            var bucket = BucketOf(male, adult);
            var opts = new List<FloatMenuOption>();
            // Save
            opts.Add(new FloatMenuOption("ASM.CondPresetSave".Translate(),
                () => Find.WindowStack.Add(new Dialog_NamePreset(name => { PresetIO.ExportConditions(name, bucket, list); Messages.Message("ASM.PresetSaved".Translate(name), MessageTypeDefOf.TaskCompletion, false); }))));
            // Load from condition presets
            var condPresets = PresetIO.ListConditionPresets(bucket);
            foreach (var pe in condPresets)
            {
                var captured = pe;
                string label = captured.name;
                if (captured.condBucket != null && captured.condBucket != bucket)
                    label += " (" + captured.condBucket + ")";
                opts.Add(new FloatMenuOption("ASM.PresetLoad".Translate() + ": " + label,
                    () =>
                    {
                        var loaded = PresetIO.ApplyConditions(captured);
                        if (loaded != null) { list.Clear(); list.AddRange(loaded); comp.MarkDirty(); Messages.Message("ASM.PresetLoaded".Translate(captured.name), MessageTypeDefOf.TaskCompletion, false); }
                    }));
            }
            // Import from kind presets (extract the matching bucket)
            foreach (var pe in PresetIO.ListPresets(PresetScope.Kind))
            {
                var captured = pe;
                opts.Add(new FloatMenuOption("ASM.CondImportKind".Translate() + ": " + captured.name,
                    () => ImportBucketFromPreset(list, captured, male, adult)));
            }
            // Import from all presets
            foreach (var pe in PresetIO.ListPresets(PresetScope.All))
            {
                var captured = pe;
                opts.Add(new FloatMenuOption("ASM.CondImportAll".Translate() + ": " + captured.name,
                    () => ImportBucketFromPreset(list, captured, male, adult)));
            }
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        private void ImportBucketFromPreset(List<SlaughterCondition> list, PresetEntry entry, bool male, bool adult)
        {
            try
            {
                var ser = new System.Xml.Serialization.XmlSerializer(typeof(SlaughterPresetDto));
                using (var r = new System.IO.StreamReader(entry.path))
                {
                    var dto = (SlaughterPresetDto)ser.Deserialize(r);
                    var kd = dto.Kinds.FirstOrDefault();
                    if (kd == null) return;
                    List<ConditionDto> source = male ? (adult ? kd.PrioAdultMale : kd.PrioYoungMale) : (adult ? kd.PrioAdultFemale : kd.PrioYoungFemale);
                    if (source == null || source.Count == 0) { Messages.Message("ASM.PresetNoSlice".Translate(animalDef.LabelCap), MessageTypeDefOf.RejectInput, false); return; }
                    list.Clear();
                    foreach (var cd in source) list.Add(cd.ToCondition());
                    comp.MarkDirty();
                    Messages.Message("ASM.PresetLoaded".Translate(entry.name), MessageTypeDefOf.TaskCompletion, false);
                }
            }
            catch { }
        }

        // ---- Condition-list section (Priorities tab, per-bucket) ----

        private float DrawConditionSection(float x, float y, float w, float listH, string titleKey,
            List<SlaughterCondition> list, ref Vector2 scroll, ref float contentHeight, ref int group, bool male, bool adult)
        {
            const float gap = 6f;
            const float btnH = 24f;
            const float iconS = 22f;

            // Row 1: title (left) + copy + paste icons right after the title.
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            float titleW = Text.CalcSize(titleKey.Translate()).x + 8f;
            Widgets.Label(new Rect(x, y, titleW, 28f), titleKey.Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            float iconX = x + titleW;
            Rect copyBtn = new Rect(iconX, y + 3f, iconS, iconS);
            TooltipHandler.TipRegion(copyBtn, "ASM.CopyConditions".Translate());
            if (Widgets.ButtonImage(copyBtn, TexButton.Copy))
                clipboard = list.Select(c => c.Clone()).ToList();
            if (HasClipboard)
            {
                Rect pasteBtn = new Rect(iconX + iconS + gap, y + 3f, iconS, iconS);
                TooltipHandler.TipRegion(pasteBtn, "ASM.PasteConditions".Translate());
                if (Widgets.ButtonImage(pasteBtn, TexButton.Paste))
                {
                    list.Clear();
                    foreach (var c in clipboard) list.Add(c.Clone());
                    comp.MarkDirty();
                }
            }
            y += 30f;

            // Row 2: add, clear, presets (left-aligned).
            float clearW = Mathf.Max(90f, Text.CalcSize("ASM.ClearList".Translate()).x + 16f);
            float addW = Mathf.Max(100f, Text.CalcSize("ASM.AddCondition".Translate()).x + 16f);
            float presetW = Mathf.Max(80f, Text.CalcSize("ASM.CondPresets".Translate()).x + 16f);
            float bx = x;
            Rect addBtn = new Rect(bx, y, addW, btnH); bx += addW + gap;
            Rect clearBtn = new Rect(bx, y, clearW, btnH); bx += clearW + gap;
            Rect presetBtn = new Rect(bx, y, presetW, btnH);

            if (Widgets.ButtonText(addBtn, "ASM.AddCondition".Translate()))
                OpenAddConditionMenu(list, male, adult);
            if (Widgets.ButtonText(presetBtn, "ASM.CondPresets".Translate()))
                Find.WindowStack.Add(new Dialog_ConditionPresetBrowser(comp, BucketOf(male, adult), list));
            GUI.enabled = list.Count > 0;
            if (Widgets.ButtonText(clearBtn, "ASM.ClearList".Translate())) { list.Clear(); comp.MarkDirty(); }
            GUI.enabled = true;
            y += btnH + 4f;

            Rect outRect = new Rect(x, y, w, listH);
            Rect view = new Rect(x, y, w - 16f, Mathf.Max(contentHeight, outRect.height));
            Widgets.BeginScrollView(outRect, ref scroll, view);
            if (Event.current.type == EventType.Repaint)
                group = ReorderableWidget.NewGroup((a, b) => ReorderList(list, a, b), ReorderableDirection.Vertical, outRect);
            float cy = y;
            for (int i = 0; i < list.Count; i++)
            {
                Rect row = new Rect(view.x, cy, view.width, 30f);
                if (i % 2 == 1) Widgets.DrawAltRect(row);
                Rect gripRect = new Rect(row.x, row.y, GripW, 30f);
                ReorderableWidget.Reorderable(group, gripRect);
                DrawConditionRow(row, list, i);
                cy += 32f;
            }
            contentHeight = Mathf.Max(cy - y, outRect.height);
            Widgets.EndScrollView();
            return y + listH;
        }

        private void DrawConditionRow(Rect row, List<SlaughterCondition> list, int index)
        {
            const float gap = 6f;
            var cond = list[index];
            // Validation: red tint on problematic rows.
            if (IsConditionProblematic(list, index))
            {
                GUI.color = new Color(0.5f, 0.15f, 0.15f, 0.5f);
                GUI.DrawTexture(row, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            Grip(row);
            // Click the label to toggle has/missing, or cycle training states.
            float labelW = row.width - TraitX - 60f;
            // Trait conditions get an inherit dropdown + copy button, shrinking the label.
            bool hasExtras = cond.type == CondType.Trait;
            if (hasExtras) labelW -= 130f; // room for inherit dropdown + copy icon
            Rect labelRect = new Rect(row.x + TraitX, row.y + 3f, labelW, 26f);
            Widgets.DrawHighlightIfMouseover(labelRect);
            if (Widgets.ButtonInvisible(labelRect))
            {
                if (cond.type == CondType.TrainingNone || cond.type == CondType.TrainingPartial || cond.type == CondType.TrainingFull)
                {
                    cond.type = cond.type == CondType.TrainingNone ? CondType.TrainingPartial
                              : cond.type == CondType.TrainingPartial ? CondType.TrainingFull
                              : CondType.TrainingNone;
                }
                else
                    cond.has = !cond.has;
                comp.MarkDirty();
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            bool wrap = Text.WordWrap;
            Text.WordWrap = false;
            Widgets.Label(new Rect(labelRect.x + 4f, labelRect.y, labelRect.width - 6f, labelRect.height), cond.Label);
            Text.WordWrap = wrap;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(labelRect, "ASM.CondToggleTip".Translate());

            // Trait-only: inherit dropdown + copy button.
            if (hasExtras)
            {
                float ex = labelRect.xMax + gap;
                Rect inheritBtn = new Rect(ex, row.y + 3f, 100f, 24f);
                InheritDropdown(inheritBtn, cond);
                Rect copyRowBtn = new Rect(inheritBtn.xMax + gap, row.y + (row.height - CopyIconS) / 2f, CopyIconS, CopyIconS);
                TooltipHandler.TipRegion(copyRowBtn, "ASM.Copy".Translate());
                if (Widgets.ButtonImage(copyRowBtn, TexButton.Copy))
                    list.Insert(index + 1, cond.Clone());
            }
            // Warning icon for problematic conditions.
            if (IsConditionProblematic(list, index))
            {
                Rect warnRect = new Rect(row.xMax - 26f - 4f - 20f, row.y + (row.height - 20f) / 2f, 20f, 20f);
                GUI.color = Color.yellow;
                GUI.DrawTexture(warnRect, TexButton.Info);
                GUI.color = Color.white;
                var conflicts = FindConflicts(list, index);
                TooltipHandler.TipRegion(warnRect, conflicts != null
                    ? "ASM.CondConflictTip".Translate(cond.Label, conflicts)
                    : "ASM.CondProblemTip".Translate(cond.Label));
            }
            if (RemoveButton(row)) { list.RemoveAt(index); comp.MarkDirty(); }
        }

        private void OpenAddConditionMenu(List<SlaughterCondition> list, bool male, bool adult)
        {
            var opts = new List<FloatMenuOption>();
            if (!male && adult)
            {
                opts.Add(new FloatMenuOption("ASM.CondPregnantHas".Translate(), () => { list.Add(new SlaughterCondition(CondType.Pregnancy) { has = true }); comp.MarkDirty(); }));
                opts.Add(new FloatMenuOption("ASM.CondPregnantMissing".Translate(), () => { list.Add(new SlaughterCondition(CondType.Pregnancy) { has = false }); comp.MarkDirty(); }));
            }
            opts.Add(new FloatMenuOption("ASM.CondBondHas".Translate(), () => { list.Add(new SlaughterCondition(CondType.Bond) { has = true }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption("ASM.CondBondMissing".Translate(), () => { list.Add(new SlaughterCondition(CondType.Bond) { has = false }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption("ASM.CondDiseaseAnyHas".Translate(), () => { list.Add(new SlaughterCondition(CondType.DiseaseAny) { has = true }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption("ASM.CondDiseaseAnyMissing".Translate(), () => { list.Add(new SlaughterCondition(CondType.DiseaseAny) { has = false }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption("ASM.CondAddTrait".Translate(), () => Find.WindowStack.Add(new Dialog_TraitPicker(picked => { foreach (var (d, has) in picked) list.Add(new SlaughterCondition(CondType.Trait) { trait = d, has = has }); comp.MarkDirty(); }))));
            opts.Add(new FloatMenuOption("ASM.CondPositiveHas".Translate(), () => { list.Add(new SlaughterCondition(CondType.HasPositiveTrait) { has = true }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption("ASM.CondPositiveMissing".Translate(), () => { list.Add(new SlaughterCondition(CondType.HasPositiveTrait) { has = false }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption("ASM.CondNegativeHas".Translate(), () => { list.Add(new SlaughterCondition(CondType.HasNegativeTrait) { has = true }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption("ASM.CondNegativeMissing".Translate(), () => { list.Add(new SlaughterCondition(CondType.HasNegativeTrait) { has = false }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption("ASM.CondAddDisease".Translate(), () => OpenDefSubmenu(list, CondType.Disease, DefDatabase<HediffDef>.AllDefs.Where(d => d.makesSickThought).OrderBy(d => d.LabelCap.ToString()), true)));
            opts.Add(new FloatMenuOption("ASM.CondAddTraining".Translate(), () => OpenTrainingSubmenu(list)));
            opts.Add(new FloatMenuOption("ASM.CondTrainingNone".Translate(), () => { list.Add(new SlaughterCondition(CondType.TrainingNone)); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption("ASM.CondTrainingPartial".Translate(), () => { list.Add(new SlaughterCondition(CondType.TrainingPartial)); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption("ASM.CondTrainingFull".Translate(), () => { list.Add(new SlaughterCondition(CondType.TrainingFull)); comp.MarkDirty(); }));
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        private void OpenDefSubmenu(List<SlaughterCondition> list, CondType ct, IEnumerable<Def> defs, bool offerMissing)
        {
            var defList = defs.ToList();
            var dupLabels = defList.GroupBy(d => d.LabelCap.ToString())
                .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
            var opts = new List<FloatMenuOption>();
            foreach (var d in defList)
            {
                var captured = d;
                string label = captured.LabelCap.ToString();
                if (dupLabels.Contains(label))
                    label += " (" + captured.defName + ")";
                opts.Add(new FloatMenuOption("ASM.CondHasSub".Translate(label),
                    () => { list.Add(MakeCond(ct, captured, true)); comp.MarkDirty(); }));
                if (offerMissing)
                    opts.Add(new FloatMenuOption("ASM.CondMissingSub".Translate(label),
                        () => { list.Add(MakeCond(ct, captured, false)); comp.MarkDirty(); }));
            }
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        private static SlaughterCondition MakeCond(CondType ct, Def d, bool has)
        {
            var c = new SlaughterCondition(ct) { has = has };
            if (ct == CondType.Trait) c.trait = d as HediffDef;
            else if (ct == CondType.Disease) c.disease = d as HediffDef;
            else if (ct == CondType.Training) c.trainable = d as TrainableDef;
            return c;
        }

        // Training submenu: available trainables for this kind first (white), unavailable below (gray).
        private void OpenTrainingSubmenu(List<SlaughterCondition> list)
        {
            var trainability = animalDef.race?.trainability;
            var opts = new List<FloatMenuOption>();
            bool Avail(TrainableDef td) => trainability != null && td.requiredTrainability != null &&
                td.requiredTrainability.intelligenceOrder <= trainability.intelligenceOrder;
            foreach (var td in DefDatabase<TrainableDef>.AllDefs.OrderBy(t => t.LabelCap.ToString()).OrderByDescending(Avail))
            {
                var captured = td;
                bool avail = Avail(captured);
                var icon = string.IsNullOrEmpty(captured.icon) ? null : captured.Icon;
                var col = avail ? Color.white : Color.gray;
                void Add(string key, bool has) =>
                    opts.Add(avail
                        ? new FloatMenuOption(key.Translate(captured.LabelCap), () => { list.Add(MakeCond(CondType.Training, captured, has)); comp.MarkDirty(); }, icon, col)
                        : new GrayFloatMenuOption(key.Translate(captured.LabelCap), () => { list.Add(MakeCond(CondType.Training, captured, has)); comp.MarkDirty(); }, icon, col, col));
                Add("ASM.CondTrainingLearnedSub", true);
                Add("ASM.CondTrainingNotSub", false);
            }
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        // FloatMenuOption with a custom GUI.color tint (for gray unavailable trainables).
        private class GrayFloatMenuOption : FloatMenuOption
        {
            private readonly Color tint;
            public GrayFloatMenuOption(string label, Action action, Texture2D iconTex, Color iconColor, Color tint)
                : base(label, action, iconTex, iconColor) { this.tint = tint; }
            public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
            {
                var prev = GUI.color;
                GUI.color = tint;
                bool r = base.DoGUI(rect, colonistOrdering, floatMenu);
                GUI.color = prev;
                return r;
            }
        }

        private void DrawExceptionsTab(float x, float y, float w, float listH)
        {
            y = DrawTraitSection(x, y, w, listH, "ASM.KeepTraits", "ASM.KeepTraitsHelp", TraitListKind.Keep,
                settings.keepTraits, ref keepScroll, ref keepListHeight, ref keepGroup, DrawKeepRow,
                list => { foreach (var d in list) settings.keepTraits.Add(NewKeep(d)); comp.MarkDirty(); });

            y = DrawBlockDivider(x, y, w);

            y = DrawTraitSection(x, y, w, listH, "ASM.ForceCullTraits", "ASM.ForceCullTraitsHelp", TraitListKind.ForceCull,
                settings.forceCullTraits, ref forceCullScroll, ref forceCullListHeight, ref forceCullGroup, DrawForceCullRow,
                list => { foreach (var d in list) settings.forceCullTraits.Add(NewCull(d)); comp.MarkDirty(); });
        }

        private static TraitTarget NewKeep(HediffDef d) => new TraitTarget(d) { inheritMode = InheritableFilter.Both, ageScope = AgeScope.Both, genderScope = GenderScope.Any, keepCount = 1 };
        private static CullTrait NewCull(HediffDef d) => new CullTrait(d) { inheritMode = InheritableFilter.Both, ageScope = AgeScope.Both, genderScope = GenderScope.Any };

        // Replace the clicked keep row: one picked trait → swap in place; several → one row per picked
        // trait, each cloning the clicked row's keep count / age / gender / inheritability.
        private void ReplaceKeepTraits(List<TraitTarget> list, TraitTarget clicked, List<HediffDef> picked)
        {
            if (picked == null || picked.Count == 0) return;
            if (picked.Count == 1) { clicked.trait = picked[0]; comp.MarkDirty(); return; }
            int idx = list.IndexOf(clicked);
            if (idx < 0) idx = list.Count; else list.RemoveAt(idx);
            for (int k = 0; k < picked.Count; k++)
                list.Insert(idx + k, new TraitTarget(picked[k]) { keepCount = clicked.keepCount, ageScope = clicked.ageScope, genderScope = clicked.genderScope, inheritMode = clicked.inheritMode });
            comp.MarkDirty();
        }

        // Same for a cull/spare/forceCull row (no keep count).
        private void ReplaceCullTraits(List<CullTrait> list, CullTrait clicked, List<HediffDef> picked)
        {
            if (picked == null || picked.Count == 0) return;
            if (picked.Count == 1) { clicked.trait = picked[0]; comp.MarkDirty(); return; }
            int idx = list.IndexOf(clicked);
            if (idx < 0) idx = list.Count; else list.RemoveAt(idx);
            for (int k = 0; k < picked.Count; k++)
                list.Insert(idx + k, new CullTrait(picked[k]) { ageScope = clicked.ageScope, genderScope = clicked.genderScope, inheritMode = clicked.inheritMode });
            comp.MarkDirty();
        }

        private float DrawPrefRow(float x, float y, float w, string labelKey, bool male, bool adult)
        {
            Rect row = new Rect(x, y, w, 30f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x, row.y, 180f, row.height), labelKey.Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            float bw = 200f;
            var cur = settings.GetPref(male, adult);
            if (ChoiceButton(new Rect(row.x + 190f, row.y + 3f, bw, row.height - 6f), "ASM.OldestFirst".Translate(), cur == SlaughterPreference.OldestFirst))
            { settings.SetPref(male, adult, SlaughterPreference.OldestFirst); comp.MarkDirty(); }
            if (ChoiceButton(new Rect(row.x + 190f + bw + 8f, row.y + 3f, bw, row.height - 6f), "ASM.YoungestFirst".Translate(), cur == SlaughterPreference.YoungestFirst))
            { settings.SetPref(male, adult, SlaughterPreference.YoungestFirst); comp.MarkDirty(); }
            return row.yMax;
        }

        // Common keep/cull/spare/force section. listH is the scroll viewport height (varies with the window).
        // listKind drives the per-list import/export buttons and the browser scope.
        private float DrawTraitSection(float x, float y, float w, float listH, string titleKey, string helpKey,
            TraitListKind listKind, IList list, ref Vector2 scroll, ref float contentHeight, ref int group,
            Action<Rect, int> drawRow, Action<List<HediffDef>> onAdd)
        {
            bool isKeep = listKind == TraitListKind.Keep;

            // Section title on the left; on the right a clear-list button, the add-trait button, and a
            // single list-preset button (which both saves and loads — one window covers export/import).
            string addKey = "ASM.AddTrait";
            const float gap = 6f;
            float presetW = Mathf.Max(96f, Text.CalcSize("ASM.TraitListPresets".Translate()).x + 18f);
            float addW = Mathf.Max(120f, Text.CalcSize(addKey.Translate()).x + 18f);
            float clearW = Mathf.Max(96f, Text.CalcSize("ASM.ClearList".Translate()).x + 18f);
            Rect presetBtn = new Rect(x + w - presetW, y + 2f, presetW, 26f);
            Rect clearBtn = new Rect(presetBtn.x - gap - clearW, y + 2f, clearW, 26f);
            Rect addBtn = new Rect(clearBtn.x - gap - addW, y + 2f, addW, 26f);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, Mathf.Max(addBtn.x - gap - x, 40f), 28f), titleKey.Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(addBtn, addKey.Translate()))
                Find.WindowStack.Add(new Dialog_TraitPicker(onAdd));
            var capListKind = listKind;
            if (Widgets.ButtonText(presetBtn, "ASM.TraitListPresets".Translate()))
                Find.WindowStack.Add(new Dialog_PresetBrowser(comp, PresetScope.List, animalDef, capListKind));
            // Clear the whole list at once (greyed out / disabled while the list is empty).
            GUI.enabled = list.Count > 0;
            if (Widgets.ButtonText(clearBtn, "ASM.ClearList".Translate())) { list.Clear(); comp.MarkDirty(); }
            GUI.enabled = true;
            TooltipHandler.TipRegion(clearBtn, "ASM.ClearList".Translate());
            y += 30f;

            if (!AnimalTraitsAccess.IsATSActive)
            {
                GUI.color = Color.yellow;
                Widgets.Label(new Rect(x, y, w, 20f), "ASM.ATSNotDetected".Translate());
                GUI.color = Color.white;
                y += 22f;
            }
            else
            {
                GUI.color = Color.gray;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(x, y, w, 20f), helpKey.Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                y += 24f;
            }

            y = DrawColumnHeaders(x, y, isKeep);

            Rect outRect = new Rect(x, y, w, listH);
            Rect view = new Rect(x, y, w - 16f, Mathf.Max(contentHeight, outRect.height));
            Widgets.BeginScrollView(outRect, ref scroll, view);
            if (Event.current.type == EventType.Repaint)
                group = ReorderableWidget.NewGroup((a, b) => ReorderList(list, a, b), ReorderableDirection.Vertical, outRect);
            float cy = y;
            for (int i = 0; i < list.Count; i++)
            {
                Rect row = new Rect(view.x, cy, view.width, 32f);
                if (i % 2 == 1) Widgets.DrawAltRect(row);
                // The grip (≡) is the reorder handle. The window is non-draggable from the lists
                // (see LateWindowOnGUI), so ReorderableWidget's own click handling is enough — do NOT
                // call ClaimDragHandle here: it uses the hint-less GetControlID, whose ID can collide
                // with a GUI.DragWindow control ID and make the window jump during the drag.
                Rect gripRect = new Rect(row.x, row.y, GripW, 32f);
                ReorderableWidget.Reorderable(group, gripRect);
                drawRow(row, i);
                cy += 34f;
            }
            contentHeight = Mathf.Max(cy - y, outRect.height);
            Widgets.EndScrollView();

            // The add-trait button moved into the section header above.
            return y + listH;
        }

        private static float DrawColumnHeaders(float x, float y, bool isKeep)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Header(x + TraitX, y, TraitWidth(isKeep), "ASM.Trait");
            if (isKeep)
                Header(x + KeepColX, y, KeepColW, "ASM.Keep");
            Header(x + AgeX, y, AgeW, "ASM.AgeScope");
            Header(x + GenderX, y, GenderW, "ASM.GenderScope");
            Header(x + InhX, y, InhW, "ASM.InheritMode");
            Text.Anchor = TextAnchor.UpperLeft;
            return y + 20f;
        }

        // Column header rendered in full (no truncation/ellipsis). Columns are sized to fit, so the
        // full word is visible — e.g. "Оставлять", "Наследуемость" in the keep section.
        private static void Header(float x, float y, float w, string key)
        {
            bool wrap = Text.WordWrap;
            Text.WordWrap = false;
            Widgets.Label(new Rect(x, y, w, 20f), key.Translate());
            Text.WordWrap = wrap;
        }

        private static bool ChoiceButton(Rect r, string label, bool selected)
            => Widgets.ButtonText(r, (selected ? "[✓] " : "[  ] ") + label);

        private void DrawKeepRow(Rect row, int index)
        {
            var tt = settings.keepTraits[index];
            Grip(row);
            TraitButton(new Rect(row.x + TraitX, row.y + 3f, TraitWidth(true), 26f), tt.trait, tt.Label,
                () => Find.WindowStack.Add(new Dialog_TraitPicker(picked => ReplaceKeepTraits(settings.keepTraits, tt, picked))));

            // Keep-count field (no per-row label — the column header already says "Оставлять").
            Text.Anchor = TextAnchor.MiddleCenter;
            string s2 = Widgets.TextField(new Rect(row.x + KeepColX + 8f, row.y + 4f, KeepColW - 16f, 24f), tt.keepCount.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            if (int.TryParse(s2, out int n) && n >= 0 && n != tt.keepCount) { tt.keepCount = n; comp.MarkDirty(); }

            Dropdown(row, AgeX, AgeW, AgeLabel(tt.ageScope), val => { tt.ageScope = val; comp.MarkDirty(); });
            Dropdown(row, GenderX, GenderW, GenderLabel(tt.genderScope), val => { tt.genderScope = val; comp.MarkDirty(); });
            Dropdown(row, InhX, InhW, InheritLabel(tt.inheritMode), val => { tt.inheritMode = val; comp.MarkDirty(); });

            Rect copyBtn = new Rect(row.xMax - 26f - 4f - CopyIconS, row.y + (row.height - CopyIconS) / 2f, CopyIconS, CopyIconS);
            TooltipHandler.TipRegion(copyBtn, "ASM.Copy".Translate());
            if (Widgets.ButtonImage(copyBtn, TexButton.Copy))
                settings.keepTraits.Insert(index + 1, new TraitTarget(tt.trait) { keepCount = tt.keepCount, ageScope = tt.ageScope, genderScope = tt.genderScope, inheritMode = tt.inheritMode });
            if (RemoveButton(row)) { settings.keepTraits.RemoveAt(index); comp.MarkDirty(); }
        }

        private void DrawCullRow(Rect row, int index) => DrawCullLikeRow(row, index, settings.cullTraits);
        private void DrawSpareRow(Rect row, int index) => DrawCullLikeRow(row, index, settings.spareTraits);
        private void DrawForceCullRow(Rect row, int index) => DrawCullLikeRow(row, index, settings.forceCullTraits);

        private void DrawCullLikeRow(Rect row, int index, List<CullTrait> list)
        {
            var ct = list[index];
            Grip(row);
            TraitButton(new Rect(row.x + TraitX, row.y + 3f, TraitWidth(false), 26f), ct.trait, ct.Label,
                () => Find.WindowStack.Add(new Dialog_TraitPicker(picked => ReplaceCullTraits(list, ct, picked))));

            Dropdown(row, AgeX, AgeW, AgeLabel(ct.ageScope), val => { ct.ageScope = val; comp.MarkDirty(); });
            Dropdown(row, GenderX, GenderW, GenderLabel(ct.genderScope), val => { ct.genderScope = val; comp.MarkDirty(); });
            Dropdown(row, InhX, InhW, InheritLabel(ct.inheritMode), val => { ct.inheritMode = val; comp.MarkDirty(); });

            Rect copyBtn = new Rect(row.xMax - 26f - 4f - CopyIconS, row.y + (row.height - CopyIconS) / 2f, CopyIconS, CopyIconS);
            TooltipHandler.TipRegion(copyBtn, "ASM.Copy".Translate());
            if (Widgets.ButtonImage(copyBtn, TexButton.Copy))
                list.Insert(index + 1, new CullTrait(ct.trait) { ageScope = ct.ageScope, genderScope = ct.genderScope, inheritMode = ct.inheritMode });
            if (RemoveButton(row)) { list.RemoveAt(index); comp.MarkDirty(); }
        }

        // Clickable trait label drawn full on one line (no truncation/ellipsis) with hover highlight + tooltip.
        private static void TraitButton(Rect r, HediffDef trait, string label, Action onClick)
        {
            Widgets.DrawHighlightIfMouseover(r);
            if (Widgets.ButtonInvisible(r)) onClick();
            GUI.color = AnimalTraitsAccess.TraitColor(trait);
            Text.Anchor = TextAnchor.MiddleLeft;
            bool wrap = Text.WordWrap;
            Text.WordWrap = false;
            Widgets.Label(new Rect(r.x + 4f, r.y, r.width - 6f, r.height), label);
            Text.WordWrap = wrap;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(r, AnimalTraitsAccess.TraitTip(trait));
        }

        // Red X remove icon, like the clear buttons in the manager table.
        private static bool RemoveButton(Rect row)
        {
            Rect b = new Rect(row.xMax - 26f, row.y + (row.height - 18f) / 2f, 18f, 18f);
            TooltipHandler.TipRegion(b, "ASM.RemoveTrait".Translate());
            GUI.color = Color.red;
            bool click = Widgets.ButtonImage(b, TexButton.CloseXSmall);
            GUI.color = Color.white;
            return click;
        }

        private static void Grip(Rect row)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(row.x + GripX, row.y, GripW, row.height), "≡");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private static void ReorderList(IList list, int from, int to)
        {
            if (from < 0 || from >= list.Count || to < 0 || to > list.Count || from == to) return;
            var item = list[from];
            list.RemoveAt(from);
            // ReorderableWidget reports `to` as an index in the ORIGINAL list. After the removal,
            // indices above `from` shift down by one, and a drop-at-end (to == oldCount) must clamp
            // to the new end — otherwise a one-row drag lands two rows down.
            if (to > list.Count) to = list.Count;
            else if (from < to) to--;
            list.Insert(to, item);
        }

        private static void Dropdown(Rect row, float x, float w, string currentLabel, Action<AgeScope> onPick)
        {
            if (Widgets.ButtonText(new Rect(row.x + x, row.y + 3f, w, 24f), currentLabel))
            {
                var opts = new List<FloatMenuOption>();
                foreach (AgeScope s in (AgeScope[])Enum.GetValues(typeof(AgeScope)))
                { var c = s; opts.Add(new FloatMenuOption(AgeLabel(s), () => onPick(c))); }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
        }

        private static void Dropdown(Rect row, float x, float w, string currentLabel, Action<GenderScope> onPick)
        {
            if (Widgets.ButtonText(new Rect(row.x + x, row.y + 3f, w, 24f), currentLabel))
            {
                var opts = new List<FloatMenuOption>();
                foreach (GenderScope s in (GenderScope[])Enum.GetValues(typeof(GenderScope)))
                { var c = s; opts.Add(new FloatMenuOption(GenderLabel(s), () => onPick(c))); }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
        }

        private static void Dropdown(Rect row, float x, float w, string currentLabel, Action<InheritableFilter> onPick)
        {
            if (Widgets.ButtonText(new Rect(row.x + x, row.y + 3f, w, 24f), currentLabel))
            {
                var opts = new List<FloatMenuOption>();
                foreach (InheritableFilter s in (InheritableFilter[])Enum.GetValues(typeof(InheritableFilter)))
                { var c = s; opts.Add(new FloatMenuOption(InheritLabel(s), () => onPick(c))); }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
        }

        private static string AgeLabel(AgeScope s)
        {
            switch (s) { case AgeScope.Adult: return "ASM.AgeAdult".Translate(); case AgeScope.Young: return "ASM.AgeYoung".Translate(); default: return "ASM.AgeBoth".Translate(); }
        }
        private static string GenderLabel(GenderScope s)
        {
            switch (s) { case GenderScope.Male: return "ASM.GenderMale".Translate(); case GenderScope.Female: return "ASM.GenderFemale".Translate(); default: return "ASM.GenderAny".Translate(); }
        }
        private static string InheritLabel(InheritableFilter s)
        {
            switch (s) { case InheritableFilter.Inheritable: return "ASM.InhInheritable".Translate(); case InheritableFilter.NonInheritable: return "ASM.InhNonInheritable".Translate(); default: return "ASM.InhBoth".Translate(); }
        }
    }
}
