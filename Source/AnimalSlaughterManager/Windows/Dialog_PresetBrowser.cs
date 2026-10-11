using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// Preset browser for the trait/settings scopes:
/// <c>All</c> — every kind (opened from the slaughter manager), <c>Kind</c> — one kind's full
/// settings, <c>List</c> — a single trait list (opened from a section's preset button). A
/// higher-scope preset can be applied to a narrower target (only the target's slice is used);
/// such presets carry a scope mark and cannot be deleted from a narrower context. The frame
/// (search/toggles/rows/save) lives in <see cref="Dialog_PresetBrowserBase"/>.
/// </summary>
public class Dialog_PresetBrowser : Dialog_PresetBrowserBase
{
    private readonly ASM_MapComp comp;
    private readonly PresetScope scope;
    private readonly ThingDef kind;
    private readonly string? listTitle;
    private readonly Action<string>? exportList;
    private readonly Func<PresetEntry, bool>? applyList;
    private readonly Func<bool>? hasListData;

    private int ContextRank => scope == PresetScope.All ? 3 : scope == PresetScope.Kind ? 2 : 1;
    private KindSettings Settings => comp.GetSettings(kind);

    public Dialog_PresetBrowser(ASM_MapComp comp, PresetScope scope, ThingDef kind)
        : this(comp, scope, kind, null, null, null, null)
    {
    }

    public Dialog_PresetBrowser(ASM_MapComp comp, PresetScope scope, ThingDef kind,
        string? listTitle, Action<string>? exportList, Func<PresetEntry, bool>? applyList,
        Func<bool>? hasListData = null)
    {
        this.comp = comp;
        this.scope = scope;
        this.kind = kind;
        this.listTitle = listTitle;
        this.exportList = exportList;
        this.applyList = applyList;
        this.hasListData = hasListData;
        optionalTitle = PresetTitle();
    }

    private string PresetTitle()
    {
        if (scope == PresetScope.List && listTitle != null)
        {
            return listTitle;
        }

        string kindLabel = kind != null ? kind.LabelCap.ToString() : "";

        switch (scope)
        {
            case PresetScope.Kind:
                return ASMKeys.PresetTitleKind.Translate(kindLabel);
            default:
                return ASMKeys.PresetTitleAll.Translate();
        }
    }

    // ---- Policy ---------------------------------------------------------------------------

    protected override bool ShowScopeToggles => scope != PresetScope.All;

    // A scope is relevant in this context if it is the context's own scope or any broader one;
    // the browser's own List scope shows the trait lists of this flavor (Keep/ForceCull).
    protected override bool ScopeVisible(PresetScope s) => ScopeRank(s) >= ContextRank;

    protected override IEnumerable<PresetEntry> ListEntries() => PresetRepository.ListPresets(PresetScope.List);

    private static int ScopeRank(PresetScope s) => s == PresetScope.All ? 3 : s == PresetScope.Kind ? 2 : 1;

    protected override bool HasData()
    {
        switch (scope)
        {
            case PresetScope.All:
                return comp.kindSettings.Values.Any(k => k != null && k.Customized);
            case PresetScope.Kind:
                return Settings.Customized;
            default:
                return hasListData?.Invoke() ?? false;
        }
    }

    protected override void Save(string name)
    {
        switch (scope)
        {
            case PresetScope.All:
                PresetRepository.ExportAll(name, comp);
                break;
            case PresetScope.Kind:
                PresetRepository.ExportKind(name, kind, Settings);
                break;
            default:
                exportList!(name);
                break;
        }
    }

    protected override bool Apply(PresetEntry entry)
    {
        switch (scope)
        {
            case PresetScope.All:
            {
                var dto = PresetRepository.ReadCarrier(entry);

                if (dto == null)
                {
                    return false;
                }

                dto.ApplyTo(comp);
                comp.MarkDirty();
                return true;
            }
            case PresetScope.Kind:
            {
                var kd = PresetRepository.ReadKindSlice(entry, kind);
                SettingsChanges.Raise();

                if (kd == null)
                {
                    return false;
                }

                kd.ApplyTo(Settings);
                return true;
            }
            default:
            {
                bool ok = applyList!(entry);
                SettingsChanges.Raise();
                return ok;
            }
        }
    }

    protected override string NoSliceLabel() => kind.LabelCap;

    // Mark cross-level presets with their scope ("[Все виды]" in a Kind/List context).
    protected override string? MarkOf(PresetEntry entry) => entry.scope != scope ? CrossLevelMark(entry) : null;

    protected override string? RowTip(PresetEntry entry) => entry.scope != scope ? CrossLevelTip(entry) : null;

    protected override bool CanOverwrite(PresetEntry entry) => entry.scope == scope && HasData();

    protected override bool CanDelete(PresetEntry entry) => entry.DeletableFrom(ContextRank);

    protected override string ConfirmDelete(PresetEntry entry) => scope == PresetScope.List
        ? ASMKeys.ConfirmDeleteList.Translate(entry.name)
        : scope == PresetScope.Kind ? ASMKeys.ConfirmDeleteKind.Translate(entry.name) : ASMKeys.ConfirmDelete.Translate(entry.name);

    private static string? CrossLevelMark(PresetEntry e)
    {
        if (e.scope == PresetScope.All)
        {
            return ASMKeys.PresetAllMark.Translate();
        }

        if (e.scope == PresetScope.Kind)
        {
            return ASMKeys.PresetKindMark.Translate();
        }

        return null;
    }

    private string CrossLevelTip(PresetEntry e)
    {
        string kindLabel = kind != null ? kind.LabelCap.ToString() : "";

        if (scope == PresetScope.Kind && e.scope == PresetScope.All)
        {
            return ASMKeys.TipAllToKind.Translate(kindLabel);
        }

        if (scope == PresetScope.List)
        {
            string ln = listTitle ?? "";

            if (e.scope == PresetScope.All)
            {
                return ASMKeys.TipAllToList.Translate(ln, kindLabel);
            }

            if (e.scope == PresetScope.Kind)
            {
                return ASMKeys.TipKindToList.Translate(ln, kindLabel);
            }
        }

        return null;
    }
}
