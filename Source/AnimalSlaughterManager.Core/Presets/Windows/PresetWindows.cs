using System;
using Verse;

namespace ASM;

/// <summary>The main assembly's preset browser windows behind the integration contract.</summary>
public sealed class PresetWindows : IPresetWindows
{
    public void OpenAllBrowser(IKindSettingsHost comp)
    {
        Find.WindowStack.Add(new Dialog_PresetBrowser((ASM_MapComp)comp, PresetScope.All, null));
    }

    public void OpenKindBrowser(IKindSettingsHost comp, ThingDef animalDef)
    {
        Find.WindowStack.Add(new Dialog_PresetBrowser((ASM_MapComp)comp, PresetScope.Kind, animalDef));
    }

    public void OpenListBrowser(IKindSettingsHost comp, ThingDef animalDef, string? title,
        Action<string> export, Func<PresetEntry, bool> apply, Func<bool> hasData)
    {
        Find.WindowStack.Add(new Dialog_PresetBrowser((ASM_MapComp)comp, PresetScope.List, animalDef, title, export, apply, hasData));
    }
}
