using Verse;

namespace ASM;

public class KindSlaughterSettingsDialogGeneralTab : BaseKindSlaughterSettingsTab
{
    public PreferenceSettingsPanel preferenceSettingsPanel;

    public override TaggedString Name => ASMKeys.TabGeneral.Translate();

    public KindSlaughterSettingsDialogGeneralTab(ASM_MapComp comp, KindPreferenceSettings settings)
    {
        void SetPreference(bool male, bool adult, SlaughterPreference value)
        {
            // TODO: create global settings class
            if (comp.GetGlobalPref(male, adult) == value)
            {
                return;
            }

            comp.SetGlobalPref(male, adult, value);
            // TODO: need this?
            settings.SetPref(male, adult, value);
            // TODO: use static class with events
            comp.MarkDirty();
        }

        preferenceSettingsPanel = new(settings, SetPreference, ASMKeys.GeneralTabHelp);
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings _settings, ASM_MapComp _comp, ThingDef _animalDef)
    {
        // TODO: pass global settings
        preferenceSettingsPanel.Draw(x, y, width);
    }
}
