using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

/// <summary>
/// Preset browser of the All scope (opened from the slaughter manager window): lists only the
/// all-animals presets. Saving writes the map's full settings; applying replaces them wholesale.
/// </summary>
public class SettingsPresetBrowserDialog : BasePresetBrowserDialog
{
    private readonly ASM_MapComp comp;

    protected override IEnumerable<PresetScope> OfferedScopes => [PresetScope.All];

    protected override bool HasData => comp.AllKindSettings.Any(k => k.Value.Customized);

    public SettingsPresetBrowserDialog(ASM_MapComp comp)
    {
        this.comp = comp;
        optionalTitle = ASMKeys.PresetTitleAll.Translate();
        targetLabel = optionalTitle;
    }

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
}
