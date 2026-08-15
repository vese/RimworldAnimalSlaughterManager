using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using UnityEngine;
using Verse;
using static UnityEngine.ParticleSystem;

namespace ASM;

public interface IKindSlaughterSettingsDialogTab
{
    void DoWindowContents(Rect inRect, ASM_MapComp comp, ThingDef animalDef, KindSettings settings, Action<float, float, float> drawTabBar);
}

public abstract class BaseKindSlaughterSettingsTab : IKindSlaughterSettingsDialogTab
{
    protected const float ScrollbarWidth = 16f;
    protected const float GapX = 6f;
    protected const float GapY = 8f;
    protected static float HeaderHeight => Text.LineHeightOf(GameFont.Medium);
    protected const float HeaderIconSize = 28f;
    protected const float HeaderMarginRight = 12f; // margin from the window close-X in the top-right corner
    protected const float LabelMarginLeft = HeaderIconSize + GapX;
    protected static float MediumTextHeight => Text.LineHeightOf(GameFont.Medium);
    protected const float ButtonHeight = 26f;
    protected const float ButtonMinWidth = 120f;
    protected const float ButtonPaddingX = 18f;
    protected static readonly float ButtonMarginTop = (HeaderHeight - ButtonHeight) / 2; //centered vertically
    protected const float TabBarHeight = 32f;
    protected const float DividerGap = 2f;
    protected static readonly Color DividerColor = new(1f, 1f, 1f, 0.25f);
    protected const float IconSize = 22f;

    protected Vector2 topScroll;

    protected virtual List<(string Text, Action<ASM_MapComp, ThingDef, KindSettings> Action)> HeaderButtons { get; } = [];
    protected virtual float ContentMinHeight { get; } = 0f;
    protected virtual float WindowMinHeight => HeaderHeight + GapY + TabBarHeight + GapY + ContentMinHeight;


    public void DoWindowContents(Rect inRect, ASM_MapComp comp, ThingDef animalDef, KindSettings settings, Action<float, float, float> drawTabBar)
    {
        if (inRect.height >= WindowMinHeight)
        {
            DrawWindow(inRect.x, inRect.y, inRect.width, inRect.height, comp, animalDef, settings, drawTabBar);
        }
        else
        {
            var width = inRect.width - ScrollbarWidth;
            Rect view = new Rect(inRect.x, inRect.y, width, WindowMinHeight);
            Widgets.BeginScrollView(inRect, ref topScroll, view);
            DrawWindow(view.x, view.y, width, inRect.height, comp, animalDef, settings, drawTabBar);
            Widgets.EndScrollView();
        }
    }

    /*
        TabDrawer.DrawTabs(new Rect(x, y, w, TabBarH), new List<TabRecord>
        {
            new TabRecord(ASMKeys.TabPriorities.Translate(), () => currentTab = 0, currentTab == 0),
            new TabRecord(ASMKeys.TabExceptions.Translate(), () => currentTab = 1, currentTab == 1),
            new TabRecord(ASMKeys.TabGeneral.Translate(), () => currentTab = 2, currentTab == 2),
        });
     */
    private void DrawWindow(float x, float y, float width, float height, ASM_MapComp comp, ThingDef animalDef, KindSettings settings, Action<float, float, float> drawTabBar)
    {
        var top = y;
        DrawHeader(x, top, width, comp, animalDef, settings);
        top += HeaderHeight + GapY;

        // Tab strip. TabDrawer draws the tab buttons in the 32px band ABOVE the base rect (they
        // hang from the rect's top edge upward), so reserve that band here and put the base rect
        // at its bottom edge — otherwise the tabs render upward into the kind-name header.
        top += TabBarHeight;
        drawTabBar(x, top, width);
        top += +GapY;

        var contentHeight = height - (HeaderHeight + GapY + TabBarHeight + GapY);
        DrawTabContent(x, top, width, contentHeight, settings, comp);
    }

    // Header: animal icon + name on the left; the per-kind preset button and the two reset
    // buttons sit to the right of the name. On the General tab they are hidden.
    protected void DrawHeader(float x, float y, float width, ASM_MapComp comp, ThingDef animalDef, KindSettings settings)
    {
        Widgets.ThingIcon(new Rect(x, y, HeaderIconSize, HeaderIconSize), animalDef);
        
        var right = x + width - HeaderMarginRight;
        var buttons = new List<(TaggedString Text, Rect Rect, Action<ASM_MapComp, ThingDef, KindSettings> Action)>(HeaderButtons.Count);

        foreach (var button in HeaderButtons)
        {
            var buttonText = button.Text.Translate();
            var buttonWidth = Mathf.Max(ButtonMinWidth, Text.CalcSize(buttonText).x + ButtonPaddingX);
            var buttonRect = new Rect(right - buttonWidth, y + ButtonMarginTop, buttonWidth, ButtonHeight);
            buttons.Add((buttonText, buttonRect, button.Action));
            right -= buttonWidth + GapX;
        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;

        Widgets.Label(new Rect(x + LabelMarginLeft, y, width, HeaderHeight), animalDef.LabelCap);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        foreach (var button in buttons)
        {
            if (Widgets.ButtonText(button.Rect, button.Text))
            {
                button.Action(comp, animalDef, settings);
            }
        }
    }

    protected abstract void DrawTabContent(float x, float y, float w, float contentHeight, KindSettings settings, ASM_MapComp comp);

    protected static void DrawDivider(float x, float y, float width)
    {
        GUI.color = DividerColor;
        Widgets.DrawLineHorizontal(x, y, width);
    }

    protected static float DrawDoubleDivider(float x, float y, float width)
    {
        var top = y;
        DrawDivider(x, top, width);
        top += DividerGap;
        DrawDivider(x, top, width);
        return top;
    }
}

public class KindSlaughterSettingsDialogPrioritiesTab : BaseKindSlaughterSettingsTab
{
    private const float ListMinHeight = 150f;

    public PreferenceSettingsPanel preferenceSettingsPanel;

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

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings settings, ASM_MapComp comp)
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
            DrawConditionSection(firstColumnX, top, listWidth, listHeight, title, setting, ref listState, key.Male, key.Adult, comp);

            if (i + 1 < KindPrioritySettings.ruleSetsNames.Count)
            {
                key = KindPrioritySettings.ruleSetsNames.Keys.ElementAt(i + 1);
                title = KindPrioritySettings.ruleSetsNames.Values.ElementAt(i + 1).Translate();
                setting = settings.prioritySettings.GetPriorityRules(key.Male, key.Adult);
                listState = PreferenceListStates[i];
                DrawConditionSection(secondColumnX, top, listWidth, listHeight, title, setting, ref listState, key.Male, key.Adult, comp);
            }
        }

        GUI.color = DividerColor;
        Widgets.DrawLineVertical(center, verticalDividerTop, listsSectionHeight);
    }

    private static List<BasePriorityRule>? clipboard;

    private bool HasClipboard => clipboard != null && clipboard.Count > 0;
    private const float ListGapY = 2f;

    private void DrawConditionSection(float x, float y, float listWidth, float listHeight, TaggedString title,
        List<BasePriorityRule> list, ref PreferenceListState listState, bool male, bool adult, ASM_MapComp comp)
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
            OpenAddConditionMenu(list, male, adult);
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
            
            DrawConditionRow(row, list, i);

            top += row.height + ListGapY;
        }

        //TODO: need this?
        //listState.contentHeight = Mathf.Max(cy - y, outRect.height);

        Widgets.EndScrollView();
    }

    private void DrawConditionRow(Rect row, List<BasePriorityRule> list, int index)
    {
        const float gap = 6f;
        var cond = list[index];
        // Validation: red tint on problematic rows.
        if (IsConditionProblematic(list, index))
        {
            GUI.color = ForceCullTint;
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
        TooltipHandler.TipRegion(labelRect, ASMKeys.CondToggleTip.Translate());

        // Trait-only: inherit dropdown + copy button.
        if (hasExtras)
        {
            float ex = labelRect.xMax + gap;
            Rect inheritBtn = new Rect(ex, row.y + 3f, 100f, 24f);
            InheritDropdown(inheritBtn, cond);
            Rect copyRowBtn = new Rect(inheritBtn.xMax + gap, row.y + (row.height - CopyIconS) / 2f, CopyIconS, CopyIconS);
            TooltipHandler.TipRegion(copyRowBtn, ASMKeys.Copy.Translate());
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
                ? ASMKeys.CondConflictTip.Translate(cond.Label, conflicts)
                : ASMKeys.CondProblemTip.Translate(cond.Label));
        }
        if (RemoveButton(row)) { list.RemoveAt(index); comp.MarkDirty(); }
    }

    private void OpenAddConditionMenu(List<BasePriorityRule> list, bool male, bool adult)
    {
        var opts = new List<FloatMenuOption>();
        if (!male && adult)
        {
            opts.Add(new FloatMenuOption(ASMKeys.CondPregnantHas.Translate(), () => { list.Add(new SlaughterCondition(CondType.Pregnancy) { has = true }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption(ASMKeys.CondPregnantMissing.Translate(), () => { list.Add(new SlaughterCondition(CondType.Pregnancy) { has = false }); comp.MarkDirty(); }));
        }
        opts.Add(new FloatMenuOption(ASMKeys.CondBondHas.Translate(), () => { list.Add(new SlaughterCondition(CondType.Bond) { has = true }); comp.MarkDirty(); }));
        opts.Add(new FloatMenuOption(ASMKeys.CondBondMissing.Translate(), () => { list.Add(new SlaughterCondition(CondType.Bond) { has = false }); comp.MarkDirty(); }));
        opts.Add(new FloatMenuOption(ASMKeys.CondDiseaseAnyHas.Translate(), () => { list.Add(new SlaughterCondition(CondType.DiseaseAny) { has = true }); comp.MarkDirty(); }));
        opts.Add(new FloatMenuOption(ASMKeys.CondDiseaseAnyMissing.Translate(), () => { list.Add(new SlaughterCondition(CondType.DiseaseAny) { has = false }); comp.MarkDirty(); }));
        // Trait options only when ATS trait content is actually loaded (the ATS dependency stub
        // alone defines no traits).
        if (AnimalTraitsAccess.HasAvailableTraits)
        {
            opts.Add(new FloatMenuOption(ASMKeys.CondAddTrait.Translate(), () => Find.WindowStack.Add(new Dialog_TraitPicker(picked => { foreach (var (d, has) in picked) list.Add(new SlaughterCondition(CondType.Trait) { trait = d, has = has }); comp.MarkDirty(); }))));
            opts.Add(new FloatMenuOption(ASMKeys.CondPositiveHas.Translate(), () => { list.Add(new SlaughterCondition(CondType.HasPositiveTrait) { has = true }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption(ASMKeys.CondPositiveMissing.Translate(), () => { list.Add(new SlaughterCondition(CondType.HasPositiveTrait) { has = false }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption(ASMKeys.CondNegativeHas.Translate(), () => { list.Add(new SlaughterCondition(CondType.HasNegativeTrait) { has = true }); comp.MarkDirty(); }));
            opts.Add(new FloatMenuOption(ASMKeys.CondNegativeMissing.Translate(), () => { list.Add(new SlaughterCondition(CondType.HasNegativeTrait) { has = false }); comp.MarkDirty(); }));
        }
        opts.Add(new FloatMenuOption(ASMKeys.CondAddDisease.Translate(), () => OpenDefSubmenu(list, CondType.Disease, DefDatabase<HediffDef>.AllDefs.Where(d => d.makesSickThought).OrderBy(d => d.LabelCap.ToString()), true)));
        opts.Add(new FloatMenuOption(ASMKeys.CondAddTraining.Translate(), () => OpenTrainingSubmenu(list)));
        opts.Add(new FloatMenuOption(ASMKeys.CondTrainingNone.Translate(), () => { list.Add(new SlaughterCondition(CondType.TrainingNone)); comp.MarkDirty(); }));
        opts.Add(new FloatMenuOption(ASMKeys.CondTrainingPartial.Translate(), () => { list.Add(new SlaughterCondition(CondType.TrainingPartial)); comp.MarkDirty(); }));
        opts.Add(new FloatMenuOption(ASMKeys.CondTrainingFull.Translate(), () => { list.Add(new SlaughterCondition(CondType.TrainingFull)); comp.MarkDirty(); }));
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
            opts.Add(new FloatMenuOption(ASMKeys.CondHasSub.Translate(label),
                () => { list.Add(MakeCond(ct, captured, true)); comp.MarkDirty(); }));
            if (offerMissing)
                opts.Add(new FloatMenuOption(ASMKeys.CondMissingSub.Translate(label),
                    () => { list.Add(MakeCond(ct, captured, false)); comp.MarkDirty(); }));
        }
        Find.WindowStack.Add(new FloatMenu(opts));
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

    private static SlaughterCondition MakeCond(CondType ct, Def d, bool has)
    {
        var c = new SlaughterCondition(ct) { has = has };
        if (ct == CondType.Trait) c.trait = d as HediffDef;
        else if (ct == CondType.Disease) c.disease = d as HediffDef;
        else if (ct == CondType.Training) c.trainable = d as TrainableDef;
        return c;
    }
}

public class KindSlaughterSettingsDialogSpecialRulesTab : BaseKindSlaughterSettingsTab
{
    protected override List<(string Text, Action<ASM_MapComp, ThingDef, KindSettings> Action)> HeaderButtons { get; } =
    [
        (ASMKeys.KindPresets, OpenKindPresetsWindow),
        (ASMKeys.ResetSpecialRules, ResetTabSettings),
        (ASMKeys.ResetKind, ResetAllSettings),
    ];

    private static void OpenKindPresetsWindow(ASM_MapComp comp, ThingDef animalDef, KindSettings _)
    {
        Find.WindowStack.Add(new Dialog_PresetBrowser(comp, PresetScope.Kind, animalDef, null));
    }

    private static void ResetTabSettings(ASM_MapComp comp, ThingDef animalDef, KindSettings settings)
    {
        settings.traitsSettings.Reset();

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

    protected override void DrawTabContent(float x, float y, float w, float contentHeight)
    {
        y = DrawTraitSection(x, y, w, listH, ASMKeys.KeepTraits, ASMKeys.KeepTraitsHelp, TraitListKind.Keep,
            settings.keepTraits, ref keepScroll, ref keepListHeight, ref keepGroup, DrawKeepRow,
            list => { foreach (var d in list) settings.keepTraits.Add(NewKeep(d)); comp.MarkDirty(); });

        y = DrawBlockDivider(x, y, w);

        y = DrawTraitSection(x, y, w, listH, ASMKeys.ForceCullTraits, ASMKeys.ForceCullTraitsHelp, TraitListKind.ForceCull,
            settings.forceCullTraits, ref forceCullScroll, ref forceCullListHeight, ref forceCullGroup, DrawForceCullRow,
            list => { foreach (var d in list) settings.forceCullTraits.Add(NewCull(d)); comp.MarkDirty(); });
    }
}

public class PreferenceSettingsPanel(float gapX, float gapY, float buttonHeight, float buttonPaddingX)
{
    private static readonly Dictionary<string, SlaughterPreference> PreferenceChoices = new()
    {
        { ASMKeys.OldestFirst, SlaughterPreference.OldestFirst },
        { ASMKeys.YoungestFirst, SlaughterPreference.YoungestFirst }
    };

    public float Draw(float x, float y, float width,
        KindSettings settings,
        ASM_MapComp comp,
        Action<KindSettings, ASM_MapComp, bool, bool, SlaughterPreference> setPreference,
        string helpTextKey)
    {
        var preferences = KindPrioritySettings.ruleSetsNames.Select(x => (x.Key, Text: x.Value.Translate()));
        var labelWidth = preferences.Max(x => Text.CalcSize(x.Text).x);
        var top = y;

        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        foreach (var preference in preferences)
        {
            DrawPreferenceRow(x, top, labelWidth, preference.Text,
                settings.preferenceSettings.GetPref(preference.Key.Male, preference.Key.Adult),
                (value) => setPreference(settings, comp, preference.Key.Male, preference.Key.Adult, value));

            top += buttonHeight + gapY;
        }

        var helpText = helpTextKey.Translate();
        var helpHeight = Text.CalcHeight(helpText, width);

        GUI.color = Color.gray;
        Text.Font = GameFont.Tiny;

        Widgets.Label(new Rect(x, top, width, helpHeight), helpText);

        top += helpHeight;

        return top;
    }

    private void DrawPreferenceRow(float x, float y, float textWidth, TaggedString label, SlaughterPreference setting, Action<SlaughterPreference> setPreference)
    {
        Widgets.Label(new Rect(x, y, textWidth, buttonHeight), label);

        var choices = PreferenceChoices.Select(x => (Text: x.Key.Translate(), Setting: x.Value));
        var buttonsWidth = choices.Max(x => Text.CalcSize(x.Text).x) + buttonPaddingX;
        var left = x + textWidth + gapX;

        foreach (var choice in choices)
        {
            if (ChoiceButton(new Rect(x + textWidth + gapX, y, buttonsWidth, buttonHeight), choice.Text, setting == choice.Setting))
            {
                setPreference(choice.Setting);
            }

            left += buttonsWidth + gapX;
        }
    }

    private static bool ChoiceButton(Rect r, string label, bool selected) =>
        Widgets.ButtonText(r, (selected ? "[✓] " : "[  ] ") + label);
}

public class KindSlaughterSettingsDialogGeneralTab : BaseKindSlaughterSettingsTab
{
    public PreferenceSettingsPanel preferenceSettingsPanel;

    public KindSlaughterSettingsDialogGeneralTab()
    {
        preferenceSettingsPanel = new PreferenceSettingsPanel(GapX, GapY, ButtonHeight, ButtonPaddingX);
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings settings, ASM_MapComp comp)
    {
        // TODO: pass global settings
        preferenceSettingsPanel.Draw(x, y, width, settings, comp, SetPreference, ASMKeys.GeneralTabHelp);
    }

    private void SetPreference(KindSettings settings, ASM_MapComp comp, bool male, bool adult, SlaughterPreference value)
    {
        if (comp.GetGlobalPref(male, adult) == value)
        {
            return;
        }

        comp.SetGlobalPref(male, adult, value);
        settings.preferenceSettings.SetPref(male, adult, value);
        comp.MarkDirty();
    }
}

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
    private Vector2 keepScroll, forceCullScroll;
    private float keepListHeight = 200f, forceCullListHeight = 120f;
    private int keepGroup = -1, forceCullGroup = -1;
    private Vector2 condScroll1, condScroll2, condScroll3, condScroll4;
    private float condListH1 = 80f, condListH2 = 80f, condListH3 = 80f, condListH4 = 80f;
    private int condGroup1 = -1, condGroup2 = -1, condGroup3 = -1, condGroup4 = -1;
    private int currentTab; // 0 = Priorities, 1 = Special rules

    // Shared UI tint colors — cached as static fields (avoids rebuilding the struct every draw
    // and dedups repeated literals).
    private static readonly Color BoldDivider = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color ForceCullTint = new Color(0.5f, 0.15f, 0.15f, 0.5f);

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

    private const float MinListH = 150f;
    // Per-section fixed chrome: section label (30) + help (22) + column headers (20). The add-trait
    // button lives in the section header row, so it adds no height here.
    private const float SectionNonListH = 30f + 22f + 20f;
    private const float CondSectionH = 60f;   // title row (30) + button row (30)
    private const float DividerGap = 8f;   // vertical room taken by a block divider line

    private readonly List<IKindSlaughterSettingsDialogTab> tabs =
    [
        new KindSlaughterSettingsDialogPrioritiesTab(),
        new KindSlaughterSettingsDialogSpecialRulesTab()
    ];

    private IKindSlaughterSettingsDialogTab currentTab1;


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
        currentTab1 = tabs.First();
    }

    public override void PreClose()
    {
        base.PreClose();

        var msg = ASMKeys.ValidationProblems.Translate(animalDef.LabelCap, settings.prioritySettings.GetErrorsCountsMessage());

        if (msg != null)
        {
            Messages.Message(msg, MessageTypeDefOf.NegativeEvent, false);
            Find.WindowStack.Add(new Dialog_MessageBox(msg));
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        currentTab1.DoWindowContents(inRect);
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

    // A thicker, more distinct separator line (for separating the help text block from
    // the age-preference rows above and the condition sections below).
    private static float DrawSectionSeparator(float x, float y, float w)
    {
        Color prev = GUI.color;
        GUI.color = BoldDivider;
        Widgets.DrawLineHorizontal(x, y + DividerGap / 2f, w);
        Widgets.DrawLineHorizontal(x, y + DividerGap / 2f + 2f, w);
        GUI.color = prev;
        return y + DividerGap;
    }

    // ---- Validation ----

    // Returns true if a condition conflicts with or duplicates another in the same list.
    // Returns true if a condition conflicts with or duplicates another in the same list.
    // Training states: 2 of 3 is fine; all 3 or exact duplicates are conflicts.
    public static bool IsConditionProblematic(List<BasePriorityRule> list, int index)
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

    private void InheritDropdown(Rect rect, SlaughterCondition cond)
    {
        string label = cond.inheritMode == TraitInheritability.Inheritable ? ASMKeys.InhInheritable.Translate()
                     : cond.inheritMode == TraitInheritability.NonInheritable ? ASMKeys.InhNonInheritable.Translate()
                     : ASMKeys.InhBoth.Translate();
        if (Widgets.ButtonText(rect, label))
        {
            var opts = new List<FloatMenuOption>();
            foreach (TraitInheritability s in (TraitInheritability[])Enum.GetValues(typeof(TraitInheritability)))
            { var c = s; opts.Add(new FloatMenuOption(InhLabel(s), () => { cond.inheritMode = c; comp.MarkDirty(); })); }
            Find.WindowStack.Add(new FloatMenu(opts));
        }
    }

    private static string InhLabel(TraitInheritability s)
    {
        switch (s) { case TraitInheritability.Inheritable: return ASMKeys.InhInheritable.Translate(); case TraitInheritability.NonInheritable: return ASMKeys.InhNonInheritable.Translate(); default: return ASMKeys.InhBoth.Translate(); }
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
            Add(ASMKeys.CondTrainingLearnedSub, true);
            Add(ASMKeys.CondTrainingNotSub, false);
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

    private static TraitProtectRule NewKeep(HediffDef d) => new TraitProtectRule(d) { inheritMode = TraitInheritability.Both, ageScope = AgeScope.Both, genderScope = GenderScope.Any, keepCount = 1 };
    private static TraitRule NewCull(HediffDef d) => new TraitRule(d) { inheritMode = TraitInheritability.Both, ageScope = AgeScope.Both, genderScope = GenderScope.Any };

    // Replace the clicked keep row: one picked trait → swap in place; several → one row per picked
    // trait, each cloning the clicked row's keep count / age / gender / inheritability.
    private void ReplaceKeepTraits(List<TraitProtectRule> list, TraitProtectRule clicked, List<HediffDef> picked)
    {
        if (picked == null || picked.Count == 0) return;
        if (picked.Count == 1) { clicked.trait = picked[0]; comp.MarkDirty(); return; }
        int idx = list.IndexOf(clicked);
        if (idx < 0) idx = list.Count; else list.RemoveAt(idx);
        for (int k = 0; k < picked.Count; k++)
            list.Insert(idx + k, new TraitProtectRule(picked[k]) { keepCount = clicked.keepCount, ageScope = clicked.ageScope, genderScope = clicked.genderScope, inheritMode = clicked.inheritMode });
        comp.MarkDirty();
    }

    // Same for a cull/spare/forceCull row (no keep count).
    private void ReplaceCullTraits(List<TraitRule> list, TraitRule clicked, List<HediffDef> picked)
    {
        if (picked == null || picked.Count == 0) return;
        if (picked.Count == 1) { clicked.trait = picked[0]; comp.MarkDirty(); return; }
        int idx = list.IndexOf(clicked);
        if (idx < 0) idx = list.Count; else list.RemoveAt(idx);
        for (int k = 0; k < picked.Count; k++)
            list.Insert(idx + k, new TraitRule(picked[k]) { ageScope = clicked.ageScope, genderScope = clicked.genderScope, inheritMode = clicked.inheritMode });
        comp.MarkDirty();
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
        string addKey = ASMKeys.AddTrait;
        const float gap = 6f;
        float presetW = Mathf.Max(96f, Text.CalcSize(ASMKeys.TraitListPresets.Translate()).x + 18f);
        float addW = Mathf.Max(120f, Text.CalcSize(addKey.Translate()).x + 18f);
        float clearW = Mathf.Max(96f, Text.CalcSize(ASMKeys.ClearList.Translate()).x + 18f);
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
        if (Widgets.ButtonText(presetBtn, ASMKeys.TraitListPresets.Translate()))
            Find.WindowStack.Add(new Dialog_PresetBrowser(comp, PresetScope.List, animalDef, capListKind));
        // Clear the whole list at once (greyed out / disabled while the list is empty).
        GUI.enabled = list.Count > 0;
        if (Widgets.ButtonText(clearBtn, ASMKeys.ClearList.Translate())) { list.Clear(); comp.MarkDirty(); }
        GUI.enabled = true;
        TooltipHandler.TipRegion(clearBtn, ASMKeys.ClearList.Translate());
        y += 30f;

        if (!AnimalTraitsAccess.HasAvailableTraits)
        {
            GUI.color = Color.yellow;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(x, y, w, 20f), ASMKeys.ATSNotDetected.Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += 24f;
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
        Header(x + TraitX, y, TraitWidth(isKeep), ASMKeys.Trait);
        if (isKeep)
            Header(x + KeepColX, y, KeepColW, ASMKeys.Keep);
        Header(x + AgeX, y, AgeW, ASMKeys.AgeScope);
        Header(x + GenderX, y, GenderW, ASMKeys.GenderScope);
        Header(x + InhX, y, InhW, ASMKeys.InheritMode);
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
        TooltipHandler.TipRegion(copyBtn, ASMKeys.Copy.Translate());
        if (Widgets.ButtonImage(copyBtn, TexButton.Copy))
            settings.keepTraits.Insert(index + 1, new TraitProtectRule(tt.trait) { keepCount = tt.keepCount, ageScope = tt.ageScope, genderScope = tt.genderScope, inheritMode = tt.inheritMode });
        if (RemoveButton(row)) { settings.keepTraits.RemoveAt(index); comp.MarkDirty(); }
    }

    private void DrawCullRow(Rect row, int index) => DrawCullLikeRow(row, index, settings.cullTraits);
    private void DrawSpareRow(Rect row, int index) => DrawCullLikeRow(row, index, settings.spareTraits);
    private void DrawForceCullRow(Rect row, int index) => DrawCullLikeRow(row, index, settings.forceCullTraits);

    private void DrawCullLikeRow(Rect row, int index, List<TraitRule> list)
    {
        var ct = list[index];
        Grip(row);
        TraitButton(new Rect(row.x + TraitX, row.y + 3f, TraitWidth(false), 26f), ct.trait, ct.Label,
            () => Find.WindowStack.Add(new Dialog_TraitPicker(picked => ReplaceCullTraits(list, ct, picked))));

        Dropdown(row, AgeX, AgeW, AgeLabel(ct.ageScope), val => { ct.ageScope = val; comp.MarkDirty(); });
        Dropdown(row, GenderX, GenderW, GenderLabel(ct.genderScope), val => { ct.genderScope = val; comp.MarkDirty(); });
        Dropdown(row, InhX, InhW, InheritLabel(ct.inheritMode), val => { ct.inheritMode = val; comp.MarkDirty(); });

        Rect copyBtn = new Rect(row.xMax - 26f - 4f - CopyIconS, row.y + (row.height - CopyIconS) / 2f, CopyIconS, CopyIconS);
        TooltipHandler.TipRegion(copyBtn, ASMKeys.Copy.Translate());
        if (Widgets.ButtonImage(copyBtn, TexButton.Copy))
            list.Insert(index + 1, new TraitRule(ct.trait) { ageScope = ct.ageScope, genderScope = ct.genderScope, inheritMode = ct.inheritMode });
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
        TooltipHandler.TipRegion(b, ASMKeys.RemoveTrait.Translate());
        GUI.color = Color.red;
        bool click = Widgets.ButtonImage(b, TexButton.CloseXSmall);
        GUI.color = Color.white;
        return click;
    }

    private static void Grip(Rect row)
    {
        GUI.color = BoldDivider;
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

    private static void Dropdown(Rect row, float x, float w, string currentLabel, Action<TraitInheritability> onPick)
    {
        if (Widgets.ButtonText(new Rect(row.x + x, row.y + 3f, w, 24f), currentLabel))
        {
            var opts = new List<FloatMenuOption>();
            foreach (TraitInheritability s in (TraitInheritability[])Enum.GetValues(typeof(TraitInheritability)))
            { var c = s; opts.Add(new FloatMenuOption(InheritLabel(s), () => onPick(c))); }
            Find.WindowStack.Add(new FloatMenu(opts));
        }
    }

    private static string AgeLabel(AgeScope s)
    {
        switch (s) { case AgeScope.Adult: return ASMKeys.AgeAdult.Translate(); case AgeScope.Young: return ASMKeys.AgeYoung.Translate(); default: return ASMKeys.AgeBoth.Translate(); }
    }
    private static string GenderLabel(GenderScope s)
    {
        switch (s) { case GenderScope.Male: return ASMKeys.GenderMale.Translate(); case GenderScope.Female: return ASMKeys.GenderFemale.Translate(); default: return ASMKeys.GenderAny.Translate(); }
    }
    private static string InheritLabel(TraitInheritability s)
    {
        switch (s) { case TraitInheritability.Inheritable: return ASMKeys.InhInheritable.Translate(); case TraitInheritability.NonInheritable: return ASMKeys.InhNonInheritable.Translate(); default: return ASMKeys.InhBoth.Translate(); }
    }
}
