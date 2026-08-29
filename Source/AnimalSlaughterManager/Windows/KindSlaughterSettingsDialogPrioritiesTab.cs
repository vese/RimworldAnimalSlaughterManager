using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

public class ListSectionState
{
    public Vector2 scroll;
    public int group;
}

public static class UIConstants
{
    public const float IconSize = 22f;
    public const float GapX = 6f;
    public const float GapY = 8f;
    public const float ButtonHeight = 26f;
    public const float ButtonMinWidth = 120f;
    public const float ButtonPaddingY = 4f;
    public const float ButtonPaddingX = 16f;
    public const float ScrollbarWidth = 16f;
    public static float SmallTextHeight => Text.LineHeightOf(GameFont.Small);
    public static float MediumTextHeight => Text.LineHeightOf(GameFont.Medium);
}

public static class KindSlaughterSettingsTabListHelper
{
    public const float ListMinHeight = 150f;
    public const float ListGapY = 2f;
    public const float ListRowPaddingX = 4f;
    private static readonly Color ListDividerColor = new(1f, 1f, 1f, 0.5f);

    public static void ReorderList(IList list, int from, int to)
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

    public static float DrawGrip(float x, float y, float height)
    {
        var anchor = Text.Anchor;
        var color = GUI.color;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = ListDividerColor;

        Widgets.Label(new Rect(x, y, UIConstants.IconSize, height), "≡");

        Text.Anchor = anchor;
        GUI.color = color;

        return UIConstants.IconSize;
    }

    // Red X remove icon, like the clear buttons in the manager table.
    public static bool RemoveButton(Rect buttonRect, string tooltipKey)
    {
        TooltipHandler.TipRegion(buttonRect, tooltipKey.Translate());

        GUI.color = Color.red;

        var click = Widgets.ButtonImage(buttonRect, TexButton.CloseXSmall);

        return click;
    }
    public static bool CopyButton(Rect buttonRect, string tooltipKey)
    {
        TooltipHandler.TipRegion(buttonRect, tooltipKey.Translate());

        GUI.color = Color.white;

        var click = Widgets.ButtonImage(buttonRect, TexButton.Copy);

        return click;
    }
}

public class KindSlaughterSettingsDialogPrioritiesTab(ASM_MapComp comp, ThingDef animalDef, KindSettings settings) : BaseKindSlaughterSettingsTab
{
    private const int ListsCountInRow = 2;

    private static readonly float ListsRowsCount = Mathf.Ceil(KindPrioritySettings.keys.Count / (float)ListsCountInRow);

    private static List<BasePriorityRule>? clipboard;

    public override TaggedString Name => ASMKeys.TabPriorities.Translate();

    protected override List<(string Text, Action<ASM_MapComp, ThingDef, KindSettings> Action)> HeaderButtons { get; } =
    [
        (ASMKeys.KindPresets, OpenKindPresetsWindow),
        (ASMKeys.ResetPriorities, ResetTabSettings),
        (ASMKeys.ResetKind, ResetAllSettings),
    ];

    private bool HasClipboard => clipboard != null && clipboard.Count > 0;

    private readonly PreferenceSettingsPanel preferenceSettingsPanel = new(settings.preferenceSettings, settings.preferenceSettings.Set, ASMKeys.PriorityHelp);

    private readonly List<ListSectionState> listsStates = [.. KindPrioritySettings.keys.Select(x => new ListSectionState())];

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings _settings, ASM_MapComp _comp, ThingDef _animalDef)
    {
        var top = preferenceSettingsPanel.Draw(x, y, width);

        top += UIConstants.GapY;

        top = DrawDoubleDivider(x, top, width);

        top += UIConstants.GapY;

        DrawConditionSections(x, top, width, contentHeight);
    }

    private void DrawConditionSections(float x, float y, float width, float contentHeight)
    {
        var top = y;
        var columnWidth = width / ListsCountInRow;
        var listWidth = columnWidth - UIConstants.GapX;
        var listsSectionHeight = contentHeight - (top - y);
        var listHeight = MathF.Max(KindSlaughterSettingsTabListHelper.ListMinHeight, (listsSectionHeight - (ListsRowsCount - 1) * 2 * UIConstants.GapY) / ListsRowsCount);
        var verticalDividerTop = top;

        for (var i = 0; i < KindPrioritySettings.keys.Count; i += ListsCountInRow)
        {
            if (i > 0)
            {
                top += listHeight + UIConstants.GapY;

                DrawDivider(x, top, width);

                top += UIConstants.GapY;
            }

            for (var column = 0; column < ListsCountInRow && i + column < KindPrioritySettings.keys.Count; column++)
            {
                var key = KindPrioritySettings.keys[i + column];
                var title = KindPrioritySettings.ruleSetsNames[key].Translate();
                var setting = settings.prioritySettings.Get(key.Male, key.Adult);
                var listState = listsStates[i + column];
                var validation = settings.prioritySettings.Validate(key.Male, key.Adult);
                var columnX = x + column * columnWidth;
                DrawConditionSection(columnX, top, listWidth, listHeight, title, setting, ref listState, key.Male, key.Adult, comp, animalDef, validation);
            }
        }

        for (var column = 1; column < ListsCountInRow; column++)
        {
            DrawVerticalDivider(x + column * columnWidth - UIConstants.GapX / 2, verticalDividerTop, listsSectionHeight);
        }
    }

    private void DrawConditionSection(float x, float y, float listWidth, float listHeight, TaggedString title,
        List<BasePriorityRule> list, ref ListSectionState listState, bool male, bool adult, ASM_MapComp comp, ThingDef animalDef, List<List<string>> validation)
    {
        // Row 1: title (left) + copy + paste icons right after the title.
        var left = x;
        var top = y;
        var titleWidth = Text.CalcSize(title).x + UIConstants.GapX;
        var titleHeight = UIConstants.MediumTextHeight;

        GUI.color = Color.white;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;

        Widgets.Label(new Rect(x, top, titleWidth, titleHeight), title);

        left += titleWidth;

        var buttonSize = UIConstants.IconSize;
        var buttonPaddingTop = (titleHeight - UIConstants.IconSize) / 2;
        var buttonTop = top + buttonPaddingTop;
        var copyButtonRect = new Rect(left, buttonTop, buttonSize, buttonSize);

        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;

        TooltipHandler.TipRegion(copyButtonRect, ASMKeys.CopyConditions.Translate());

        if (Widgets.ButtonImage(copyButtonRect, TexButton.Copy))
        {
            clipboard = [.. list.Select(c => c.Clone())];
        }

        left += buttonSize + UIConstants.GapX;

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
        var addButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(addButtonText).x + UIConstants.ButtonPaddingX);
        var presetButtonText = ASMKeys.CondPresets.Translate();
        var presetButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(presetButtonText).x + UIConstants.ButtonPaddingX);
        var clearButtonText = ASMKeys.ClearList.Translate();
        var clearButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(clearButtonText).x + UIConstants.ButtonPaddingX);

        Rect addBtn = new Rect(left, top, addButtonWidth, UIConstants.ButtonHeight);
        left += addButtonWidth + UIConstants.GapX;
        Rect presetBtn = new Rect(left, top, presetButtonWidth, UIConstants.ButtonHeight);
        left += presetButtonWidth + UIConstants.GapX;
        Rect clearBtn = new Rect(left, top, clearButtonWidth, UIConstants.ButtonHeight);

        if (Widgets.ButtonText(addBtn, addButtonText))
        {
            OpenAddConditionMenu(list, male, adult, comp, animalDef);
        }

        if (Widgets.ButtonText(presetBtn, presetButtonText))
        {
            Find.WindowStack.Add(new Dialog_ConditionPresetBrowser(comp, male, adult, list));
        }

        GUI.enabled = list.Count > 0;

        if (Widgets.ButtonText(clearBtn, clearButtonText))
        {
            // TODO: changes in list in settings class, use ReadonlyList
            list.Clear();
            comp.MarkDirty();
        }

        GUI.enabled = true;

        top += UIConstants.ButtonHeight + UIConstants.GapY;

        // List
        left = x;

        var outRect = new Rect(left, top, listWidth, listHeight);
        var view = new Rect(left, top, listWidth - UIConstants.ScrollbarWidth, outRect.height/*TODO: need this? Mathf.Max(listState.contentHeight, outRect.height)*/);

        Widgets.BeginScrollView(outRect, ref listState.scroll, view);

        if (Event.current.type == EventType.Repaint)
        {
            // TODO: changes in list in settings class, use ReadonlyList
            listState.group = ReorderableWidget.NewGroup((a, b) => KindSlaughterSettingsTabListHelper.ReorderList(list, a, b), ReorderableDirection.Vertical, outRect);
        }

        for (int i = 0; i < list.Count; i++)
        {
            var row = new Rect(view.x, top, view.width, UIConstants.MediumTextHeight);

            if (i % 2 == 1)
            {
                Widgets.DrawAltRect(row);
            }

            ReorderableWidget.Reorderable(listState.group, new Rect(row.x, row.y, UIConstants.IconSize, row.height));

            DrawConditionRow(row, list, i, validation, comp);

            top += row.height + KindSlaughterSettingsTabListHelper.ListGapY;
        }

        //TODO: need this?
        //listState.contentHeight = Mathf.Max(cy - y, outRect.height);

        Widgets.EndScrollView();
    }

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

        var left = row.x;
        var top = row.y;

        left += KindSlaughterSettingsTabListHelper.DrawGrip(left, top, row.height);
        left += UIConstants.GapX;

        var labelWidth = row.width - UIConstants.IconSize - UIConstants.IconSize - UIConstants.IconSize;
        var condition = list[index];

        if (condition.HasExtraParameters)
        {
            labelWidth -= UIConstants.ButtonMinWidth;// TODO: size (min of max from names length or half of available width)
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = Color.white;

        top = row.y + (UIConstants.MediumTextHeight - UIConstants.ButtonHeight) / 2;
        var labelButtonRect = new Rect(left, top, labelWidth, UIConstants.ButtonHeight);

        Widgets.DrawHighlightIfMouseover(labelButtonRect);

        if (Widgets.ButtonInvisible(labelButtonRect))
        {
            condition.ChangeVariant();
            comp.MarkDirty();
        }

        var wrap = Text.WordWrap;
        Text.WordWrap = false;

        var labelRect = new Rect(
            labelButtonRect.x + KindSlaughterSettingsTabListHelper.ListRowPaddingX,
            labelButtonRect.y,
            labelButtonRect.width - KindSlaughterSettingsTabListHelper.ListRowPaddingX,
            labelButtonRect.height);
        Widgets.Label(labelRect, condition.Label);

        Text.WordWrap = wrap;

        Text.Anchor = TextAnchor.UpperLeft;

        TooltipHandler.TipRegion(labelButtonRect, ASMKeys.CondToggleTip.Translate());

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
            var warnRect = new Rect(
                row.xMax - UIConstants.GapX - UIConstants.IconSize - UIConstants.IconSize,
                row.y + (row.height - UIConstants.IconSize) / 2,
                UIConstants.IconSize,
                UIConstants.IconSize);

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

        top = row.y + (row.height - UIConstants.IconSize) / 2f;
        left = row.xMax - UIConstants.IconSize;
        var removeButtonRect = new Rect(left, top, UIConstants.IconSize, UIConstants.IconSize);

        if (KindSlaughterSettingsTabListHelper.RemoveButton(removeButtonRect, ASMKeys.RemoveTrait))
        {
            // TODO: changes in list in settings class, use ReadonlyList
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

    private static void OpenKindPresetsWindow(ASM_MapComp comp, ThingDef animalDef, KindSettings _)
    {
        Find.WindowStack.Add(new Dialog_PresetBrowser(comp, PresetScope.Kind, animalDef/*, null*/));
    }

    private static void ResetTabSettings(ASM_MapComp comp, ThingDef animalDef, KindSettings settings)
    {
        settings.preferenceSettings.Reset(comp.globalSettings.preferenceSettings);
        settings.prioritySettings.Reset();

        comp.MarkDirty();
    }

    private static void ResetAllSettings(ASM_MapComp comp, ThingDef animalDef, KindSettings settings)
    {
        settings.Reset(comp.globalSettings);

        comp.MarkDirty();
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
}
