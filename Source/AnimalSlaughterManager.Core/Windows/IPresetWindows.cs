using System;
using Verse;

namespace ASM;

/// <summary>
/// The preset browser windows — implemented by the main assembly (they live with the preset
/// repository); integrations open them through this contract instead of referencing the
/// window types.
/// </summary>
public interface IPresetWindows
{
    /// <summary>Opens the All-scope browser.</summary>
    void OpenAllBrowser(IKindSettingsHost comp);

    /// <summary>Opens the Kind-scope browser for one kind.</summary>
    void OpenKindBrowser(IKindSettingsHost comp, ThingDef animalDef);

    /// <summary>Opens the List-scope browser for a rule list (title, save/apply/hasData
    /// wired to that list).</summary>
    void OpenListBrowser(IKindSettingsHost comp, ThingDef animalDef, string? title,
        Action<string> export, Func<PresetEntry, bool> apply, Func<bool> hasData);
}
