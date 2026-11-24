
using System.Collections.Generic;
using Verse;

namespace ASM;

public class GeneralSettingsTab : BaseSettingsTab
{
    public override TaggedString Name => ASMKeys.TabGeneral.Translate();

    private readonly PreferenceSettingsPanel preferenceSettingsPanel;

    public GeneralSettingsTab(Dictionary<ThingDef, KindSettings> kindSettings, GlobalSettings settings)
    {
        void SetPreference(bool male, bool adult, SlaughterPreference value)
        {
            settings.preferenceSettings.Set(male, adult, value);

            foreach (var ks in kindSettings.Values)
            {
                ks?.preferenceSettings?.Set(male, adult, value);
            }
        }

        preferenceSettingsPanel = new(settings.preferenceSettings, SetPreference, ASMKeys.GeneralTabHelp);
    }

    protected override void DrawTabContent(float x, float y, float width, float contentHeight, KindSettings _settings, ISettingsProvider _comp, ThingDef _animalDef)
    {
        preferenceSettingsPanel.Draw(x, y, width);
    }
}
