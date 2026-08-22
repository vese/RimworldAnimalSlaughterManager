using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

public class KindSlaughterSettingsDialogPrioritiesTab : BaseKindSlaughterSettingsTab
{
    private const float ListMinHeight = 150f;

    public PreferenceSettingsPanel preferenceSettingsPanel;

    public override TaggedString Name => ASMKeys.TabPriorities.Translate();

    protected override List<(string Text, Action<ASM_MapComp, ThingDef, KindSettings> Action)> HeaderButtons { get; } =
    [
        (ASMKeys.KindPresets, OpenKindPresetsWindow),
        (ASMKeys.ResetPriorities, ResetTabSettings),
        (ASMKeys.ResetKind, ResetAllSettings),
    ];

    public KindSlaughterSettingsDialogPrioritiesTab()
    {
        preferenceSettingsPanel = new PreferenceSettingsPanel(GapX, GapY, ButtonHeight, ButtonPaddingX);
    }

    private static void OpenKindPresetsWindow(ASM_MapComp comp, ThingDef animalDef, KindSettings _)
    {
        Find.WindowStack.Add(new Dialog_PresetBrowser(comp, PresetScope.Kind, animalDef, null));
    }

    private static void ResetTabSettings(ASM_MapComp comp, ThingDef animalDef, KindSettings settings)
    {
        // TODO: settings.preferenceSettings.Reset()
        settings.preferenceSettings.malePref = comp.globalMalePref;
        settings.preferenceSettings.femalePref = comp.globalFemalePref;
        settings.preferenceSettings.maleYoungPref = comp.globalMaleYoungPref;
        settings.preferenceSettings.femaleYoungPref = comp.globalFemaleYoungPref;
        settings.prioritySettings.Reset();

        comp.MarkDirty();
    }

    private static void ResetAllSettings(ASM_MapComp comp, ThingDef animalDef, KindSettings settings)
    {
        // TODO: settings.preferenceSettings.Reset()
        settings.preferenceSettings.malePref = comp.globalMalePref;
        settings.preferenceSettings.femalePref = comp.globalFemalePref;
        settings.preferenceSettings.maleYoungPref = comp.globalMaleYoungPref;
        settings.preferenceSettings.femaleYoungPref = comp.globalFemaleYoungPref;
        settings.prioritySettings.Reset();
        settings.traitsSettings.Reset();

        comp.MarkDirty();
    }

    private void SetPreference(KindSettings settings, ASM_MapComp comp, bool male, bool adult, SlaughterPreference value)
    {
        if (settings.preferenceSettings.GetPref(male, adult) == value)
        {
            return;
        }

        settings.preferenceSettings.SetPref(male, adult, value);
        comp.MarkDirty();
    }

    private readonly List<PreferenceListState> PreferenceListStates = [.. KindPrioritySettings.ruleSetsNames.Select(x => new PreferenceListState())];

    public class PreferenceListState
    {
        public Vector2 scroll;
        public float contentHeight;
        public int group;
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings settings, ASM_MapComp comp, ThingDef animalDef)
    {
        var top = preferenceSettingsPanel.Draw(x, y, width, settings, comp, SetPreference, ASMKeys.PriorityHelp);

        top += GapY;

        top += DrawDoubleDivider(x, top, width);

        top += GapY;

        var halfWidth = width / 2;
        var center = x + halfWidth;
        var firstColumnX = x;
        var secondColumnX = center + GapX;
        var listWidth = halfWidth - GapX;
        var listsSectionHeight = contentHeight - (top - y);
        var listHeight = MathF.Max(ListMinHeight, (listsSectionHeight - GapY - GapY) / 2);
        var verticalDividerTop = top;

        for (var i = 0; i < KindPrioritySettings.ruleSetsNames.Count; i += 2)
        {
            if (i > 0)
            {
                top += GapY;

                DrawDivider(x, top, width);

                top += listHeight + GapY;
            }

            var key = KindPrioritySettings.ruleSetsNames.Keys.ElementAt(i);
            var title = KindPrioritySettings.ruleSetsNames.Values.ElementAt(i).Translate();
            var setting = settings.prioritySettings.GetPriorityRules(key.Male, key.Adult);
            var listState = PreferenceListStates[i];
            var validation = settings.prioritySettings.Validate(key.Male, key.Adult);
            DrawConditionSection(firstColumnX, top, listWidth, listHeight, title, setting, ref listState, key.Male, key.Adult, comp, animalDef, validation);

            if (i + 1 < KindPrioritySettings.ruleSetsNames.Count)
            {
                key = KindPrioritySettings.ruleSetsNames.Keys.ElementAt(i + 1);
                title = KindPrioritySettings.ruleSetsNames.Values.ElementAt(i + 1).Translate();
                setting = settings.prioritySettings.GetPriorityRules(key.Male, key.Adult);
                listState = PreferenceListStates[i];
                validation = settings.prioritySettings.Validate(key.Male, key.Adult);
                DrawConditionSection(secondColumnX, top, listWidth, listHeight, title, setting, ref listState, key.Male, key.Adult, comp, animalDef, validation);
            }
        }

        GUI.color = DividerColor;
        Widgets.DrawLineVertical(center, verticalDividerTop, listsSectionHeight);
    }

    private static List<BasePriorityRule>? clipboard;

    private bool HasClipboard => clipboard != null && clipboard.Count > 0;
    private const float ListGapY = 2f;
    private static readonly Color ListDividerColor = new(1f, 1f, 1f, 0.5f);

    private void DrawConditionSection(float x, float y, float listWidth, float listHeight, TaggedString title,
        List<BasePriorityRule> list, ref PreferenceListState listState, bool male, bool adult, ASM_MapComp comp, ThingDef animalDef, List<List<string>> validation)
    {
        // Row 1: title (left) + copy + paste icons right after the title.
        var left = x;
        var top = y;
        var titleWidth = Text.CalcSize(title).x + GapX;
        var titleHeight = MediumTextHeight;

        GUI.color = Color.white;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;

        Widgets.Label(new Rect(x, top, titleWidth, titleHeight), title);

        left += titleWidth;
        var buttonSize = IconSize;
        var buttonPaddingTop = (titleHeight - IconSize) / 2;
        var buttonTop = top + buttonPaddingTop;
        var copyButtonRect = new Rect(left, buttonTop, buttonSize, buttonSize);

        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;

        TooltipHandler.TipRegion(copyButtonRect, ASMKeys.CopyConditions.Translate());

        if (Widgets.ButtonImage(copyButtonRect, TexButton.Copy))
        {
            clipboard = [.. list.Select(c => c.Clone())];
        }

        left += buttonSize + GapX;

        if (HasClipboard)
        {
            var pasteButtonRect = new Rect(left, buttonTop, buttonSize, buttonSize);

            TooltipHandler.TipRegion(pasteButtonRect, ASMKeys.PasteConditions.Translate());

            if (Widgets.ButtonImage(pasteButtonRect, TexButton.Paste))
            {
                list.Clear();

                foreach (var c in clipboard!)
                {
                    list.Add(c.Clone());
                }

                comp.MarkDirty();
            }
        }

        top += titleHeight;

        // Row 2: add, clear, presets (left-aligned).
        left = x;

        var addButtonText = ASMKeys.AddCondition.Translate();
        var addButtonWidth = Mathf.Max(ButtonMinWidth, Text.CalcSize(addButtonText).x + ButtonPaddingX);
        var clearButtonText = ASMKeys.ClearList.Translate();
        var clearButtonWidth = Mathf.Max(ButtonMinWidth, Text.CalcSize(clearButtonText).x + ButtonPaddingX);
        var presetButtonText = ASMKeys.CondPresets.Translate();
        var presetButtonWidth = Mathf.Max(ButtonMinWidth, Text.CalcSize(presetButtonText).x + ButtonPaddingX);

        Rect addBtn = new Rect(left, top, addButtonWidth, ButtonHeight);
        left += addButtonWidth + GapX;
        Rect clearBtn = new Rect(left, top, clearButtonWidth, ButtonHeight);
        left += clearButtonWidth + GapX;
        Rect presetBtn = new Rect(left, top, presetButtonWidth, ButtonHeight);

        if (Widgets.ButtonText(addBtn, addButtonText))
        {
            OpenAddConditionMenu(list, male, adult, comp, animalDef);
        }

        if (Widgets.ButtonText(presetBtn, ASMKeys.CondPresets.Translate()))
        {
            Find.WindowStack.Add(new Dialog_ConditionPresetBrowser(comp, male, adult, list));
        }

        GUI.enabled = list.Count > 0;

        if (Widgets.ButtonText(clearBtn, ASMKeys.ClearList.Translate()))
        {
            list.Clear();
            comp.MarkDirty();
        }

        GUI.enabled = true;

        top += ButtonHeight + GapY;

        // List
        left = x;

        var outRect = new Rect(left, top, listWidth, listHeight);
        var view = new Rect(left, top, listWidth - ScrollbarWidth, outRect.height/*TODO: need this? Mathf.Max(listState.contentHeight, outRect.height)*/);

        Widgets.BeginScrollView(outRect, ref listState.scroll, view);

        if (Event.current.type == EventType.Repaint)
        {
            listState.group = ReorderableWidget.NewGroup((a, b) => ReorderList(list, a, b), ReorderableDirection.Vertical, outRect);
        }

        for (int i = 0; i < list.Count; i++)
        {
            var row = new Rect(view.x, top, view.width, MediumTextHeight);
            
            if (i % 2 == 1)
            {
                Widgets.DrawAltRect(row);
            }

            ReorderableWidget.Reorderable(listState.group, new Rect(row.x, row.y, IconSize, row.height));
            
            DrawConditionRow(row, list, i, validation, comp);

            top += row.height + ListGapY;
        }

        //TODO: need this?
        //listState.contentHeight = Mathf.Max(cy - y, outRect.height);

        Widgets.EndScrollView();
    }

    private const float ListRowPaddingX = 4f;

    private void DrawConditionRow(Rect row, List<BasePriorityRule> list, int index, List<List<string>> validation, ASM_MapComp comp)
    {
        var rowValidation = validation[index];

        if (rowValidation is not null && rowValidation.Count > 0)
        {
            // TODO: need this?
            //private static readonly Color ForceCullTint = new Color(0.5f, 0.15f, 0.15f, 0.5f);
            //GUI.color = ForceCullTint;
            //GUI.DrawTexture(row, Texture2D.whiteTexture);
            //GUI.color = Color.white;
        }

        DrawGrip(row);

        var labelWidth = row.width - IconSize - IconSize - IconSize;
        var condition = list[index];

        if (condition.HasExtraParameters)
        {
            labelWidth -= ButtonMinWidth;//todo
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = Color.white;

        var labelRect = new Rect(row.x + IconSize, row.y + (MediumTextHeight - ButtonHeight) / 2, labelWidth, ButtonHeight);

        Widgets.DrawHighlightIfMouseover(labelRect);

        if (Widgets.ButtonInvisible(labelRect))
        {
            condition.ChangeVariant();
            comp.MarkDirty();
        }

        var wrap = Text.WordWrap;
        Text.WordWrap = false;

        Widgets.Label(new Rect(labelRect.x + ListRowPaddingX, labelRect.y, labelRect.width - ListRowPaddingX, labelRect.height), condition.Label);

        Text.WordWrap = wrap;

        Text.Anchor = TextAnchor.UpperLeft;

        TooltipHandler.TipRegion(labelRect, ASMKeys.CondToggleTip.Translate());

        // TODO:
        // Trait-only: inherit dropdown + copy button.
        //if (condition.HasExtraParameters)
        //{
        //    float ex = labelRect.xMax + gap;
        //    Rect inheritBtn = new Rect(ex, row.y + 3f, 100f, 24f);
        //    InheritDropdown(inheritBtn, cond);
        //    Rect copyRowBtn = new Rect(inheritBtn.xMax + gap, row.y + (row.height - CopyIconS) / 2f, CopyIconS, CopyIconS);
        //    TooltipHandler.TipRegion(copyRowBtn, ASMKeys.Copy.Translate());
        //    if (Widgets.ButtonImage(copyRowBtn, TexButton.Copy))
        //        list.Insert(index + 1, cond.Clone());
        //}

        //private void InheritDropdown(Rect rect, SlaughterCondition cond)
        //{
        //    string label = cond.inheritMode == TraitInheritability.Inheritable ? ASMKeys.InhInheritable.Translate()
        //                 : cond.inheritMode == TraitInheritability.NonInheritable ? ASMKeys.InhNonInheritable.Translate()
        //                 : ASMKeys.InhBoth.Translate();
        //    if (Widgets.ButtonText(rect, label))
        //    {
        //        var opts = new List<FloatMenuOption>();
        //        foreach (TraitInheritability s in (TraitInheritability[])Enum.GetValues(typeof(TraitInheritability)))
        //        { var c = s; opts.Add(new FloatMenuOption(InhLabel(s), () => { cond.inheritMode = c; comp.MarkDirty(); })); }
        //        Find.WindowStack.Add(new FloatMenu(opts));
        //    }
        //}

        //private static string InhLabel(TraitInheritability s)
        //{
        //    switch (s) { case TraitInheritability.Inheritable: return ASMKeys.InhInheritable.Translate(); case TraitInheritability.NonInheritable: return ASMKeys.InhNonInheritable.Translate(); default: return ASMKeys.InhBoth.Translate(); }
        //}

        // Warning icon for problematic conditions.
        if (rowValidation is not null && rowValidation.Count > 0)
        {
            var warnRect = new Rect(row.xMax - GapX - IconSize - IconSize, row.y + (row.height - IconSize) / 2, IconSize, IconSize);

            GUI.color = Color.yellow;

            GUI.DrawTexture(warnRect, TexButton.Info);

            GUI.color = Color.white;

            // TODO:
            //var conflicts = FindConflicts(list, index);
            var tipText = //conflicts != null ?
                //ASMKeys.CondConflictTip.Translate(cond.Label, conflicts) :
                ASMKeys.CondProblemTip.Translate(condition.Label);

            TooltipHandler.TipRegion(warnRect, tipText);
        }

        if (RemoveButton(row))
        {
            list.RemoveAt(index);
            comp.MarkDirty();
        }
    }

    //// Returns a comma-separated list of conflicting condition labels, or null if none found.
    //public static string FindConflicts(List<SlaughterCondition> list, int index)
    //{
    //    var c = list[index];
    //    var conflicts = new List<string>();
    //    bool isTrainingState = c.type == CondType.TrainingNone || c.type == CondType.TrainingPartial || c.type == CondType.TrainingFull;
    //
    //    if (!isTrainingState)
    //    {
    //        string key = ConditionKey(c);
    //        for (int i = 0; i < list.Count; i++)
    //        {
    //            if (i == index) continue;
    //            if (ConditionKey(list[i]) == key)
    //                conflicts.Add(list[i].Label);
    //        }
    //    }
    //    else
    //    {
    //        for (int i = 0; i < list.Count; i++)
    //        {
    //            if (i == index) continue;
    //            var ot = list[i].type;
    //            if (ot == CondType.TrainingNone || ot == CondType.TrainingPartial || ot == CondType.TrainingFull)
    //                if (!conflicts.Contains(list[i].Label))
    //                    conflicts.Add(list[i].Label);
    //        }
    //    }
    //    return conflicts.Count > 0 ? string.Join(", ", conflicts.ToArray()) : null;
    //}

    private void OpenAddConditionMenu(List<BasePriorityRule> list, bool male, bool adult, ASM_MapComp comp, ThingDef animalDef)
    {
        var options = new List<FloatMenuOption>();

        if (!male && adult)
        {
            // TODO: 1 option
            options.Add(new FloatMenuOption(ASMKeys.CondPregnantHas.Translate(),
                () => { list.Add(new PregnancyPriorityRule() { has = true }); comp.MarkDirty(); }));
            options.Add(new FloatMenuOption(ASMKeys.CondPregnantMissing.Translate(),
                () => { list.Add(new PregnancyPriorityRule() { has = false }); comp.MarkDirty(); }));
        }

        // TODO: 1 option
        options.Add(new FloatMenuOption(ASMKeys.CondBondHas.Translate(),
            () => { list.Add(new BondPriorityRule() { has = true }); comp.MarkDirty(); }));
        options.Add(new FloatMenuOption(ASMKeys.CondBondMissing.Translate(),
            () => { list.Add(new BondPriorityRule() { has = false }); comp.MarkDirty(); }));

        // TODO: cache
        var diseaseList = DefDatabase<HediffDef>.AllDefs.Where(d => d.makesSickThought).OrderBy(d => d.LabelCap.ToString()).ToList();
        options.Add(new FloatMenuOption(ASMKeys.CondAddDisease.Translate(),
            () => OpenDiseaseSubmenu(list, diseaseList, comp)));
        // TODO: 1 option
        options.Add(new FloatMenuOption(ASMKeys.CondDiseaseAnyHas.Translate(),
            () => { list.Add(new DiseaseAnyPriorityRule() { has = true }); comp.MarkDirty(); }));
        options.Add(new FloatMenuOption(ASMKeys.CondDiseaseAnyMissing.Translate(),
            () => { list.Add(new DiseaseAnyPriorityRule() { has = false }); comp.MarkDirty(); }));
        
        options.Add(new FloatMenuOption(ASMKeys.CondAddTraining.Translate(),
            () => OpenTrainingSubmenu(list, animalDef, comp)));
        options.Add(new FloatMenuOption(ASMKeys.CondTrainingNone.Translate(),
            () => { list.Add(new TrainingGeneralPriorityRule()); comp.MarkDirty(); }));
        options.Add(new FloatMenuOption(ASMKeys.CondTrainingPartial.Translate(),
            () => { list.Add(new TrainingGeneralPriorityRule()); comp.MarkDirty(); }));
        options.Add(new FloatMenuOption(ASMKeys.CondTrainingFull.Translate(),
            () => { list.Add(new TrainingGeneralPriorityRule()); comp.MarkDirty(); }));

        // Trait options only when ATS trait content is actually loaded.
        if (AnimalTraitsAccess.HasAvailableTraits)
        {
            options.Add(new FloatMenuOption(ASMKeys.CondAddTrait.Translate(),
                () => Find.WindowStack.Add(new Dialog_TraitPicker(picked =>
                {
                    // TODO: 1 option for 1 trait def
                    foreach ((HediffDef def, bool has) in picked)
                    {
                        list.Add(new TraitPriorityRule() { has = has, trait = def });
                    }

                    comp.MarkDirty();
                }))));
            // TODO: 1 option with TraitType.Both
            options.Add(new FloatMenuOption(ASMKeys.CondPositiveHas.Translate(),
                () => { list.Add(new TraitGeneralPriorityRule() { has = true, type = TraitType.Positive }); comp.MarkDirty(); }));
            options.Add(new FloatMenuOption(ASMKeys.CondPositiveMissing.Translate(),
                () => { list.Add(new TraitGeneralPriorityRule() { has = false, type = TraitType.Positive }); comp.MarkDirty(); }));
            options.Add(new FloatMenuOption(ASMKeys.CondNegativeHas.Translate(),
                () => { list.Add(new TraitGeneralPriorityRule() { has = true, type = TraitType.Negative }); comp.MarkDirty(); }));
            options.Add(new FloatMenuOption(ASMKeys.CondNegativeMissing.Translate(),
                () => { list.Add(new TraitGeneralPriorityRule() { has = false, type = TraitType.Negative }); comp.MarkDirty(); }));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void OpenDiseaseSubmenu(List<BasePriorityRule> list, List<HediffDef> defs, ASM_MapComp comp)
    {
        // TODO: cache
        var dupLabels = defs
            .GroupBy(d => d.LabelCap.ToString())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        var options = new List<FloatMenuOption>();

        foreach (var def in defs)
        {
            var label = def.LabelCap.ToString();

            if (dupLabels.Contains(label))
            {
                label = $"{label} ({def.defName})";
            }

            options.Add(new FloatMenuOption(ASMKeys.CondHasSub.Translate(label),
                () => { list.Add(new DiseasePriorityRule() { has = true, disease = def }); comp.MarkDirty(); }));

            // TODO: 1 option
            options.Add(new FloatMenuOption(ASMKeys.CondMissingSub.Translate(label),
                () => { list.Add(new DiseasePriorityRule() { has = false, disease = def }); comp.MarkDirty(); }));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    // Training submenu: available trainables for this kind first (white), unavailable below (gray).
    private void OpenTrainingSubmenu(List<BasePriorityRule> list, ThingDef animalDef, ASM_MapComp comp)
    {
        var trainability = animalDef.race?.trainability;

        var options = new List<FloatMenuOption>();

        bool Avail(TrainableDef td) => trainability != null && td.requiredTrainability != null &&
            td.requiredTrainability.intelligenceOrder <= trainability.intelligenceOrder;

        // TODO: cache list
        foreach (var def in DefDatabase<TrainableDef>.AllDefs.OrderBy(t => t.LabelCap.ToString()).OrderByDescending(Avail))
        {
            var avail = Avail(def);
            var icon = string.IsNullOrEmpty(def.icon) ? null : def.Icon;
            var col = avail ? Color.white : Color.gray;

            void Add(string key, bool has) =>
                options.Add(avail ?
                    new FloatMenuOption(key.Translate(def.LabelCap),
                    () => { list.Add(new TrainingPriorityRule() { has = has, trainable = def }); comp.MarkDirty(); }, icon, col) :
                    new GrayFloatMenuOption(key.Translate(def.LabelCap),
                    () => { list.Add(new TrainingPriorityRule() { has = has, trainable = def }); comp.MarkDirty(); }, icon, col, col));
            
            Add(ASMKeys.CondTrainingLearnedSub, true);
            // TODO: 1 option
            Add(ASMKeys.CondTrainingNotSub, false);
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    // TODO: move to file
    // FloatMenuOption with a custom GUI.color tint (for gray unavailable trainables).
    private class GrayFloatMenuOption : FloatMenuOption
    {
        private readonly Color tint;

        public GrayFloatMenuOption(string label, Action action, Texture2D? icon, Color iconColor, Color tint) : base(label, action, icon, iconColor)
        {
            this.tint = tint;
        }

        public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
        {
            var prev = GUI.color;
            GUI.color = tint;
            var click = base.DoGUI(rect, colonistOrdering, floatMenu);
            GUI.color = prev;
            return click;
        }
    }

    private static void ReorderList(IList list, int from, int to)
    {
        if (from < 0 || from >= list.Count || to < 0 || to > list.Count || from == to)
        {
            return;
        }

        var item = list[from];

        list.RemoveAt(from);
        
        if (from < to)
        {
            list.Insert(to - 1, item);
        }
        else
        {
            list.Insert(to, item);
        }
    }

    private static void DrawGrip(Rect row)
    {
        GUI.color = ListDividerColor;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(row.x, row.y, IconSize, row.height), "≡");
    }

    // Red X remove icon, like the clear buttons in the manager table.
    private static bool RemoveButton(Rect row)
    {
        var buttonRect = new Rect(row.xMax - IconSize - GapX, row.y + (row.height - IconSize) / 2f, IconSize, IconSize);

        TooltipHandler.TipRegion(buttonRect, ASMKeys.RemoveTrait.Translate());

        GUI.color = Color.red;

        var click = Widgets.ButtonImage(buttonRect, TexButton.CloseXSmall);

        return click;
    }
}
