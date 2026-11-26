using System.Collections.Generic;
using System.Linq;

using Verse;

namespace ASM;

/// <summary>
/// Preset browser of the All scope (opened from the slaughter manager window): lists only the
/// all-animals presets. Saving writes the map's full settings; applying replaces them wholesale.
/// </summary>
public class Dialog_AllPresetBrowser : Dialog_PresetBrowser
{
    private readonly ASM_MapComp comp;

    public Dialog_AllPresetBrowser(ASM_MapComp comp)
    {
        this.comp = comp;
        optionalTitle = ASMKeys.PresetTitleAll.Translate();
    }

    protected override IEnumerable<(PresetScope Scope, IEnumerable<PresetEntry> Entries)> ScopeSources()
    {
        yield return (PresetScope.All, PresetRepository.ListPresets(PresetScope.All));
    }

    protected override bool HasData() => comp.kindSettings.Values.Any(k => k != null && k.Customized);

    protected override void Save(string name) => PresetRepository.Save(name, PresetScope.All, comp.ToPreset());

    protected override bool Apply(PresetEntry entry)
    {
        if (PresetRepository.ReadAll(entry) is not { } dto)
        {
            return false;
        }

        comp.FromPreset(dto);
        return true;
    }

    protected override string NoSliceLabel() => ASMKeys.PresetTitleAll.Translate();
}
