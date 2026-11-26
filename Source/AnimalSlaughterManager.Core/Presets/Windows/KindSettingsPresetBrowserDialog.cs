using System.Collections.Generic;

using Verse;

namespace ASM;

/// <summary>
/// Preset browser of the Kind scope (the kind window's preset buttons): lists this kind's
/// presets plus the All presets — an All row carries its scope mark, and applying takes only
/// the slice of this kind.
/// </summary>
public class KindSettingsPresetBrowserDialog : BasePresetBrowserDialog
{
    private readonly ThingDef kind;
    private readonly KindSettings settings;

    protected override (PresetScope Scope, IEnumerable<PresetEntry> Entries)[] ScopeSources =>
    [
        (PresetScope.Kind, PresetRepository.ListPresets(PresetScope.Kind)),
        (PresetScope.All, PresetRepository.ListPresets(PresetScope.All))
    ];

    protected internal override bool HasData => settings.Customized;

    public KindSettingsPresetBrowserDialog(ThingDef kind, KindSettings settings)
    {
        this.kind = kind;
        this.settings = settings;
        targetLabel = kind.LabelCap;
        optionalTitle = ASMKeys.PresetTitleKind.Translate(targetLabel);
    }

    protected internal override void Save(string name) =>
        PresetRepository.Save(name, PresetScope.Kind, new KindPresetFile { Kind = settings.ToPreset(kind) });

    protected internal override bool Apply(PresetEntry entry)
    {
        var kd = PresetRepository.ReadKind(entry, kind);

        if (kd == null)
        {
            return false;
        }

        settings.FromPreset(kd.Kind);

        return true;
    }

    // Kind/All rows carry their scope mark ("[Все виды]"); only the kind's slice is applied.
    protected internal override string? GetRowMark(PresetEntry entry) =>
        entry.scope == PresetScope.All ? ASMKeys.PresetAllMark.Translate() : null;

    protected internal override string? GetRowTip(PresetEntry entry) =>
        entry.scope == PresetScope.All ? ASMKeys.TipAllToKind.Translate(targetLabel) : null;

    protected internal override bool CanOverwrite(PresetEntry entry) => entry.scope == PresetScope.Kind && HasData;

    protected internal override bool CanDelete(PresetEntry entry) => entry.DeletableFrom(PresetScope.Kind);

    protected internal override string GetConfirmDeleteText(PresetEntry entry) => ASMKeys.ConfirmDeleteKind.Translate(entry.name);
}
