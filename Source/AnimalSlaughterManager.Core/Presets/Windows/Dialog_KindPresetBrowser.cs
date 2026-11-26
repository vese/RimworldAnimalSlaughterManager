using System.Collections.Generic;

using Verse;

namespace ASM;

/// <summary>
/// Preset browser of the Kind scope (the kind window's preset buttons): lists this kind's
/// presets plus the All presets — an All row carries its scope mark, and applying takes only
/// the slice of this kind.
/// </summary>
public class Dialog_KindPresetBrowser : Dialog_PresetBrowser
{
    private readonly ASM_MapComp comp;
    private readonly ThingDef kind;

    public Dialog_KindPresetBrowser(ASM_MapComp comp, ThingDef kind)
    {
        this.comp = comp;
        this.kind = kind;
        optionalTitle = ASMKeys.PresetTitleKind.Translate(kind.LabelCap.ToString());
    }

    private KindSettings Settings => comp.GetSettings(kind);

    protected override IEnumerable<(PresetScope Scope, IEnumerable<PresetEntry> Entries)> ScopeSources()
    {
        yield return (PresetScope.Kind, PresetRepository.ListPresets(PresetScope.Kind));
        yield return (PresetScope.All, PresetRepository.ListPresets(PresetScope.All));
    }

    protected override bool HasData() => Settings.Customized;

    protected override void Save(string name) =>
        PresetRepository.Save(name, PresetScope.Kind, new KindPresetFile { Kind = Settings.ToPreset(kind) });

    protected override bool Apply(PresetEntry entry)
    {
        var kd = PresetRepository.ReadKind(entry, kind);
        SettingsChangeTracker.Raise();

        if (kd == null)
        {
            return false;
        }

        Settings.FromPreset(kd.Kind);
        return true;
    }

    protected override string NoSliceLabel() => kind.LabelCap;

    // Kind/All rows carry their scope mark ("[Все виды]"); only the kind's slice is applied.
    protected override string? MarkOf(PresetEntry entry) => entry.scope == PresetScope.All
        ? ASMKeys.PresetAllMark.Translate()
        : null;

    protected override string? RowTip(PresetEntry entry) => entry.scope == PresetScope.All
        ? ASMKeys.TipAllToKind.Translate(kind.LabelCap.ToString())
        : null;

    protected override bool CanOverwrite(PresetEntry entry) => entry.scope == PresetScope.Kind && HasData();

    protected override bool CanDelete(PresetEntry entry) => entry.DeletableFrom(PresetScope.Kind);

    protected override string ConfirmDelete(PresetEntry entry) => ASMKeys.ConfirmDeleteKind.Translate(entry.name);
}
