using System;
using System.Collections.Generic;

using Verse;

namespace ASM;

/// <summary>
/// Preset browser of the List scope (a section's preset button): lists the list presets plus
/// Kind/All presets — such rows carry their scope mark, and applying takes only the slice of
/// this list. The list's own behavior (its folder, export, apply and has-data checks) comes
/// from the opening section.
/// </summary>
public class SpecialSettingsListPresetBrowserDialog : BasePresetBrowserDialog
{
    private readonly ThingDef kind;
    private readonly string listTitle;
    private readonly Action<string> exportList;
    private readonly Func<PresetEntry, bool> applyList;
    private readonly Func<bool>? hasListData;

    protected override (PresetScope Scope, IEnumerable<PresetEntry> Entries)[] ScopeSources =>
    [
        (PresetScope.List, PresetRepository.ListPresets(PresetScope.List)),
        (PresetScope.Kind, PresetRepository.ListPresets(PresetScope.Kind)),
        (PresetScope.All, PresetRepository.ListPresets(PresetScope.All))
    ];

    protected override bool HasData => hasListData?.Invoke() ?? false;

    public SpecialSettingsListPresetBrowserDialog(ThingDef kind, string titleKey,
        Action<string> exportList, Func<PresetEntry, bool> applyList, Func<bool>? hasListData = null)
    {
        this.kind = kind;
        this.exportList = exportList;
        this.applyList = applyList;
        this.hasListData = hasListData;
        targetLabel = kind.LabelCap;
        listTitle = titleKey.Translate(targetLabel);
        optionalTitle = listTitle;
    }

    protected override void Save(string name) => exportList(name);

    protected override bool Apply(PresetEntry entry) => applyList(entry);

    // Kind/All rows carry their scope mark; only the slice of this list is applied.
    protected override string? GetRowMark(PresetEntry entry)
    {
        if (entry.scope == PresetScope.All)
        {
            return ASMKeys.PresetAllMark.Translate();
        }

        if (entry.scope == PresetScope.Kind)
        {
            return ASMKeys.PresetKindMark.Translate();
        }

        return null;
    }

    protected override string? GetRowTip(PresetEntry entry)
    {
        string kindLabel = kind.LabelCap.ToString();

        if (entry.scope == PresetScope.All)
        {
            return ASMKeys.TipAllToList.Translate(listTitle, kindLabel);
        }

        if (entry.scope == PresetScope.Kind)
        {
            return ASMKeys.TipKindToList.Translate(listTitle, kindLabel);
        }

        return null;
    }

    protected override bool CanOverwrite(PresetEntry entry) => entry.scope == PresetScope.List && HasData;

    protected override bool CanDelete(PresetEntry entry) => entry.DeletableFrom(PresetScope.List);

    protected override string ConfirmDelete(PresetEntry entry) => ASMKeys.ConfirmDeleteList.Translate(entry.name);
}
