using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

public class KindSlaughterSettingsDialogPrioritiesTab(ASM_MapComp comp, ThingDef animalDef, KindSettings settings) : BaseKindSlaughterSettingsTab
{
    private const int ListsCountInRow = 2;

    private static readonly float ListsRowsCount = Mathf.Ceil(KindPrioritySettings.keys.Count / (float)ListsCountInRow);
    private static readonly float ListsRowsGapYSum = (ListsRowsCount - 1) * 2 * UIConstants.GapY;

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

        var conditionsSectionsHeight = contentHeight - (top - y);
        
        DrawConditionSections(x, top, width, conditionsSectionsHeight);
    }

    private void DrawConditionSections(float x, float y, float width, float contentHeight)
    {
        var top = y;
        var gapBetweenLists = 2 * UIConstants.GapX;
        var listWidth = (width - (ListsCountInRow - 1) * gapBetweenLists) / ListsCountInRow;
        var listHeight = MathF.Max(UIConstants.ListMinHeight, (contentHeight - ListsRowsGapYSum) / ListsRowsCount);
        var verticalDividerTop = top;

        for (var i = 0; i < KindPrioritySettings.keys.Count; i += ListsCountInRow)
        {
            if (i > 0)
            {
                top += listHeight + UIConstants.GapY;

                DrawDivider(x, top, width);

                top += UIConstants.GapY;
            }

            var left = x;

            for (var column = 0; column < ListsCountInRow && i + column < KindPrioritySettings.keys.Count; column++)
            {
                var key = KindPrioritySettings.keys[i + column];
                var title = KindPrioritySettings.ruleSetsNames[key].Translate();
                var setting = settings.prioritySettings.Get(key.Male, key.Adult);
                var listState = listsStates[i + column];
                var validation = settings.prioritySettings.Validate(key.Male, key.Adult);

                DrawConditionSection(left, top, listWidth, listHeight, title, setting, ref listState, key.Male, key.Adult, validation);

                left += listWidth + gapBetweenLists;
            }
        }

        for (var column = 1; column < ListsCountInRow; column++)
        {
            DrawVerticalDivider(x + column * (listWidth + gapBetweenLists) - UIConstants.GapX, verticalDividerTop, contentHeight);
        }
    }

    private void DrawConditionSection(float x, float y, float listWidth, float listHeight, TaggedString title,
        List<BasePriorityRule> list, ref ListSectionState listState, bool male, bool adult, List<List<string>> validation)
    {
        var left = x;
        var top = y;

        // Row 1: title (left) + copy + paste icons right after the title.
        top += DrawConditionSectionHeader(x, y, title, list, male, adult);

        // Row 2: add, clear, presets (left-aligned).
        top += UIConstants.GapY;

        top += DrawButtons(left, top, list, male, adult);

        // List
        top += UIConstants.GapY;

        DrawConditionList(left, top, listWidth, listHeight, list, ref listState, male, adult, validation);
    }

    private float DrawConditionSectionHeader(float x, float y, TaggedString title, List<BasePriorityRule> rules, bool male, bool adult)
    {
        var color = GUI.color;
        GUI.color = Color.white;

        (var titleWidth, var height) = DrawConditionSectionHeaderTitle(x, y, title);
        DrawConditionSectionHeaderButtons(x + titleWidth, y, height, rules, male, adult);

        GUI.color = color;

        return height;
    }

    private (float width, float height) DrawConditionSectionHeaderTitle(float x, float y, TaggedString title)
    {
        var font = Text.Font;
        var anchor = Text.Anchor;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperLeft;

        var rectWidth = Text.CalcSize(title).x + UIConstants.GapX;
        var rectHeight = Text.LineHeight + UIConstants.TextPaddingY;

        Widgets.Label(new Rect(x, y, rectWidth, rectHeight), title);

        Text.Font = font;
        Text.Anchor = anchor;

        return (rectWidth, rectHeight);
    }

    private void DrawConditionSectionHeaderButtons(float x, float y, float height, List<BasePriorityRule> list, bool male, bool adult)
    {
        var font = Text.Font;
        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;

        var left = x;
        var copyButtonRect = new Rect(left, y, height, height);

        if (Widgets.ButtonImage(copyButtonRect, TexButton.Copy, tooltip: ASMKeys.CopyConditions.Translate()))
        {
            clipboard = [.. list.Select(c => c.Clone())];
        }

        left += height + UIConstants.GapX;

        if (HasClipboard)
        {
            var pasteButtonRect = new Rect(left, y, height, height);

            if (Widgets.ButtonImage(pasteButtonRect, TexButton.Paste, tooltip: ASMKeys.PasteConditions.Translate()))
            {
                settings.prioritySettings.ReplaceAll(male, adult, clipboard!.Select(c => c.Clone()).ToList());
            }
        }

        Text.Font = font;
        Text.Anchor = anchor;
    }

    private float DrawButtons(float x, float y, List<BasePriorityRule> list, bool male, bool adult)
    {
        var color = GUI.color;
        var font = Text.Font;
        var anchor = Text.Anchor;
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        var left = x;
        var height = Text.LineHeight + UIConstants.ButtonPaddingY;

        var addButtonText = ASMKeys.AddCondition.Translate();
        var addButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(addButtonText).x + UIConstants.ButtonPaddingX);
        var addBtn = new Rect(left, y, addButtonWidth, height);
        left += addButtonWidth + UIConstants.GapX;

        if (Widgets.ButtonText(addBtn, addButtonText))
        {
            OpenAddConditionMenu(male, adult, animalDef);
        }

        var presetButtonText = ASMKeys.CondPresets.Translate();
        var presetButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(presetButtonText).x + UIConstants.ButtonPaddingX);
        var presetBtn = new Rect(left, y, presetButtonWidth, height);
        left += presetButtonWidth + UIConstants.GapX;

        if (Widgets.ButtonText(presetBtn, presetButtonText))
        {
            Find.WindowStack.Add(new Dialog_ConditionPresetBrowser(comp, male, adult, settings.prioritySettings));
        }

        var clearButtonText = ASMKeys.ClearList.Translate();
        var clearButtonWidth = Mathf.Max(UIConstants.ButtonMinWidth, Text.CalcSize(clearButtonText).x + UIConstants.ButtonPaddingX);
        var clearBtn = new Rect(left, y, clearButtonWidth, height);

        GUI.enabled = list.Count > 0;

        if (Widgets.ButtonText(clearBtn, clearButtonText))
        {
            settings.prioritySettings.Clear(male, adult);
        }

        GUI.enabled = true;

        GUI.color = color;
        Text.Font = font;
        Text.Anchor = anchor;

        return height;
    }

    private void DrawConditionList(float x, float y, float listWidth, float listHeight,
        List<BasePriorityRule> list, ref ListSectionState listState, bool male, bool adult, List<List<string>> validation)
    {
        var top = y;
        var rowHeight = Text.LineHeight + UIConstants.TextPaddingY;
        var contentHeight = list.Count * (rowHeight + KindSlaughterSettingsTabListHelper.ListGapY);
        var outRect = new Rect(x, top, listWidth, listHeight);
        var view = new Rect(x, top, listWidth - UIConstants.ScrollbarWidth, Mathf.Max(contentHeight, outRect.height));

        Widgets.BeginScrollView(outRect, ref listState.scroll, view);

        if (Event.current.type == EventType.Repaint)
        {
            listState.group = ReorderableWidget.NewGroup((a, b) => settings.prioritySettings.Move(male, adult, a, b), ReorderableDirection.Vertical, outRect);
        }

        for (int i = 0; i < list.Count; i++)
        {
            var row = new Rect(view.x, top, view.width, rowHeight);

            if (i % 2 == 1)
            {
                Widgets.DrawAltRect(row);
            }

            ReorderableWidget.Reorderable(listState.group, new Rect(row.x, row.y, UIConstants.IconSize, row.height));

            DrawConditionRow(row, list, i, validation, male, adult);

            top += row.height + KindSlaughterSettingsTabListHelper.ListGapY;
        }

        Widgets.EndScrollView();
    }

    private void DrawConditionRow(Rect row, List<BasePriorityRule> list, int index, List<List<string>> validation, bool male, bool adult)
    {
        var rowValidation = validation[index];

        if (rowValidation is not null && rowValidation.Count > 0)
        {
            var prevColor = GUI.color;
            GUI.color = UIConstants.ValidationProblemTint;
            GUI.DrawTexture(row, BaseContent.WhiteTex);
            GUI.color = prevColor;
            TooltipHandler.TipRegion(row, string.Join("\n", rowValidation));
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

        top = row.y + (row.height - UIConstants.ButtonHeight) / 2f;
        var labelButtonRect = new Rect(left, top, labelWidth, UIConstants.ButtonHeight);

        Widgets.DrawHighlightIfMouseover(labelButtonRect);

        if (Widgets.ButtonInvisible(labelButtonRect))
        {
            settings.prioritySettings.ChangeVariant(male, adult, index);
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
            settings.prioritySettings.RemoveAt(male, adult, index);
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

    private void OpenAddConditionMenu(bool male, bool adult, ThingDef animalDef)
    {
        var options = new List<FloatMenuOption>();

        if (!male && adult)
        {
            // TODO: 1 option
            options.Add(new FloatMenuOption(ASMKeys.CondPregnantHas.Translate(),
                () => settings.prioritySettings.Add(male, adult, new PregnancyPriorityRule() { has = true })));
            options.Add(new FloatMenuOption(ASMKeys.CondPregnantMissing.Translate(),
                () => settings.prioritySettings.Add(male, adult, new PregnancyPriorityRule() { has = false })));
        }

        // TODO: 1 option
        options.Add(new FloatMenuOption(ASMKeys.CondBondHas.Translate(),
            () => settings.prioritySettings.Add(male, adult, new BondPriorityRule() { has = true })));
        options.Add(new FloatMenuOption(ASMKeys.CondBondMissing.Translate(),
            () => settings.prioritySettings.Add(male, adult, new BondPriorityRule() { has = false })));

        // TODO: cache
        var diseaseList = DefDatabase<HediffDef>.AllDefs.Where(d => d.makesSickThought).OrderBy(d => d.LabelCap.ToString()).ToList();
        options.Add(new FloatMenuOption(ASMKeys.CondAddDisease.Translate(),
            () => OpenDiseaseSubmenu(male, adult, diseaseList)));
        // TODO: 1 option
        options.Add(new FloatMenuOption(ASMKeys.CondDiseaseAnyHas.Translate(),
            () => settings.prioritySettings.Add(male, adult, new DiseaseAnyPriorityRule() { has = true })));
        options.Add(new FloatMenuOption(ASMKeys.CondDiseaseAnyMissing.Translate(),
            () => settings.prioritySettings.Add(male, adult, new DiseaseAnyPriorityRule() { has = false })));

        options.Add(new FloatMenuOption(ASMKeys.CondAddTraining.Translate(),
            () => OpenTrainingSubmenu(male, adult, animalDef)));
        options.Add(new FloatMenuOption(ASMKeys.CondTrainingNone.Translate(),
            () => settings.prioritySettings.Add(male, adult, new TrainingGeneralPriorityRule())));
        options.Add(new FloatMenuOption(ASMKeys.CondTrainingPartial.Translate(),
            () => settings.prioritySettings.Add(male, adult, new TrainingGeneralPriorityRule())));
        options.Add(new FloatMenuOption(ASMKeys.CondTrainingFull.Translate(),
            () => settings.prioritySettings.Add(male, adult, new TrainingGeneralPriorityRule())));

        // Trait options only when ATS trait content is actually loaded.
        if (AnimalTraitsAccess.HasAvailableTraits)
        {
            options.Add(new FloatMenuOption(ASMKeys.CondAddTrait.Translate(),
                () => Find.WindowStack.Add(new Dialog_TraitPicker(picked =>
                {
                    // TODO: 1 option for 1 trait def
                    foreach ((HediffDef def, bool has) in picked)
                    {
                        settings.prioritySettings.Add(male, adult, new TraitPriorityRule() { has = has, trait = def });
                    }
                }))));
            // TODO: 1 option with TraitType.Both
            options.Add(new FloatMenuOption(ASMKeys.CondPositiveHas.Translate(),
                () => settings.prioritySettings.Add(male, adult, new TraitGeneralPriorityRule() { has = true, type = TraitType.Positive })));
            options.Add(new FloatMenuOption(ASMKeys.CondPositiveMissing.Translate(),
                () => settings.prioritySettings.Add(male, adult, new TraitGeneralPriorityRule() { has = false, type = TraitType.Positive })));
            options.Add(new FloatMenuOption(ASMKeys.CondNegativeHas.Translate(),
                () => settings.prioritySettings.Add(male, adult, new TraitGeneralPriorityRule() { has = true, type = TraitType.Negative })));
            options.Add(new FloatMenuOption(ASMKeys.CondNegativeMissing.Translate(),
                () => settings.prioritySettings.Add(male, adult, new TraitGeneralPriorityRule() { has = false, type = TraitType.Negative })));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void OpenDiseaseSubmenu(bool male, bool adult, List<HediffDef> defs)
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
                () => settings.prioritySettings.Add(male, adult, new DiseasePriorityRule() { has = true, disease = def })));

            // TODO: 1 option
            options.Add(new FloatMenuOption(ASMKeys.CondMissingSub.Translate(label),
                () => settings.prioritySettings.Add(male, adult, new DiseasePriorityRule() { has = false, disease = def })));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    // Training submenu: available trainables for this kind first (white), unavailable below (gray).
    private void OpenTrainingSubmenu(bool male, bool adult, ThingDef animalDef)
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
                    () => settings.prioritySettings.Add(male, adult, new TrainingPriorityRule() { has = has, trainable = def }), icon, col) :
                    new GrayFloatMenuOption(key.Translate(def.LabelCap),
                    () => settings.prioritySettings.Add(male, adult, new TrainingPriorityRule() { has = has, trainable = def }), icon, col, col));

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
    }

    private static void ResetAllSettings(ASM_MapComp comp, ThingDef animalDef, KindSettings settings)
    {
        settings.Reset(comp.globalSettings);
    }

}
