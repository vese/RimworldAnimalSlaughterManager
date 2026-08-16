using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ASM;

public class KindSlaughterSettingsDialogSpecialRulesTab : BaseKindSlaughterSettingsTab
{
    public TaggedString Name => ASMKeys.TabExceptions.Translate();

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

    private static TraitProtectRule NewKeep(HediffDef d) => new TraitProtectRule(d) { inheritMode = TraitInheritability.Both, ageScope = AgeScope.Both, genderScope = GenderScope.Any, keepCount = 1 };
    private static TraitRule NewCull(HediffDef d) => new TraitRule(d) { inheritMode = TraitInheritability.Both, ageScope = AgeScope.Both, genderScope = GenderScope.Any };

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
    private const float GripX = 0f, GripW = 22f;
    private const float GenderX = 478f, GenderW = 96f;
    private const float InhX = 576f, InhW = 144f;
    private const float CopyIconS = 20f;

    private const float TraitX = 24f;
    private const float KeepColX = 286f, KeepColW = 86f;   // keep-count (keep rows only)
    private const float AgeX = 372f, AgeW = 104f;
    private static float TraitWidth(bool isKeep) => isKeep ? (KeepColX - TraitX) : (AgeX - TraitX);
}
