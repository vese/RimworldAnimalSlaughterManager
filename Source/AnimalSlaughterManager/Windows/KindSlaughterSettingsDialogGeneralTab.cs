using Verse;

namespace ASM;

public class KindSlaughterSettingsDialogGeneralTab : BaseKindSlaughterSettingsTab
{
    public PreferenceSettingsPanel preferenceSettingsPanel;

    public override TaggedString Name => ASMKeys.TabGeneral.Translate();

    public KindSlaughterSettingsDialogGeneralTab(ASM_MapComp comp, GlobalSettings settings)
    {
        void SetPreference(bool male, bool adult, SlaughterPreference value)
        {
            if (settings.preferenceSettings.Get(male, adult) == value)
            {
                return;
            }

            settings.preferenceSettings.Set(male, adult, value);
            // TODO: use static class with events
            comp.MarkDirty();
        }

        preferenceSettingsPanel = new(settings.preferenceSettings, SetPreference, ASMKeys.GeneralTabHelp);
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings _settings, ASM_MapComp _comp, ThingDef _animalDef)
    {
        preferenceSettingsPanel.Draw(x, y, width);
    }
}
