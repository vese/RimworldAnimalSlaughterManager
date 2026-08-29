using Verse;

namespace ASM;

public class KindSlaughterSettingsDialogGeneralTab : BaseKindSlaughterSettingsTab
{
    public PreferenceSettingsPanel preferenceSettingsPanel;

    public override TaggedString Name => ASMKeys.TabGeneral.Translate();

    public KindSlaughterSettingsDialogGeneralTab(GlobalSettings settings)
    {
        preferenceSettingsPanel = new(settings.preferenceSettings, settings.preferenceSettings.Set, ASMKeys.GeneralTabHelp);
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings _settings, ASM_MapComp _comp, ThingDef _animalDef)
    {
        preferenceSettingsPanel.Draw(x, y, width);
    }
}
