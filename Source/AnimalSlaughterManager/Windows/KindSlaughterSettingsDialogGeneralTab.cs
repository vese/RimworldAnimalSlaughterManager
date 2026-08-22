using Verse;

namespace ASM;

public class KindSlaughterSettingsDialogGeneralTab : BaseKindSlaughterSettingsTab
{
    public PreferenceSettingsPanel preferenceSettingsPanel;

    public override TaggedString Name => ASMKeys.TabGeneral.Translate();

    public KindSlaughterSettingsDialogGeneralTab()
    {
        preferenceSettingsPanel = new PreferenceSettingsPanel(UIConstants.GapX, UIConstants.GapY, UIConstants.ButtonHeight, UIConstants.ButtonPaddingX);
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings settings, ASM_MapComp comp, ThingDef animalDef)
    {
        // TODO: pass global settings
        preferenceSettingsPanel.Draw(x, y, width, settings, comp, SetPreference, ASMKeys.GeneralTabHelp);
    }

    private void SetPreference(KindSettings settings, ASM_MapComp comp, bool male, bool adult, SlaughterPreference value)
    {
        if (comp.GetGlobalPref(male, adult) == value)
        {
            return;
        }

        comp.SetGlobalPref(male, adult, value);
        settings.preferenceSettings.SetPref(male, adult, value);
        comp.MarkDirty();
    }
}
