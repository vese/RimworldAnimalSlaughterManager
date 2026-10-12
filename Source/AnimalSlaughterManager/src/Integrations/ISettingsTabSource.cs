namespace ASM;

/// <summary>Contributes a per-kind tab to the kind settings window; null = no tab.</summary>
public interface ISettingsTabSource
{
    IKindSlaughterSettingsDialogTab? CreateTab(KindSettings settings);
}
