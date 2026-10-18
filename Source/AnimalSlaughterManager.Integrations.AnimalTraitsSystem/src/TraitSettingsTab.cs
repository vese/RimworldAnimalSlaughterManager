namespace ASM;

/// <summary>The per-kind "Special rules" tab with the ATS keep/force-cull lists.</summary>
public sealed class TraitSettingsTab : ISettingsTabSource
{
    public IKindSlaughterSettingsDialogTab? CreateTab(KindSettings settings) =>
        new KindSlaughterSettingsDialogSpecialRulesTab(KindTraitsSettings.Of(settings));
}
