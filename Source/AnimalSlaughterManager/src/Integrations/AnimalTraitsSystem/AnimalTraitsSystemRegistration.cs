using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// Self-registration of the Animal Traits System integration: RimWorld runs every
/// [StaticConstructorOnStartup] type, so the integration registers its services when its
/// foreign mod is active — the core resolves them through the registry contracts only.
/// </summary>
[StaticConstructorOnStartup]
public static class AnimalTraitsSystemRegistration
{
    static AnimalTraitsSystemRegistration()
    {
        // The kind-settings provider is registered unconditionally: the settings data
        // survives ATS being disabled (the types live in this assembly).
        IntegrationRegistry.Add<IKindIntegrationSettingsProvider>(new KindTraitsSettings.Provider());

        if (!ModsConfig.IsActive(AnimalTraitsIntegration.PackageId))
        {
            return;
        }

        var integration = new AnimalTraitsIntegration();
        IntegrationRegistry.Add<IRuleOptionsSource>(integration);
        IntegrationRegistry.Add<ISettingsTabSource>(integration);
        IntegrationRegistry.Add<IPawnSlaughterSignals>(integration);
        IntegrationRegistry.Add<IKeepReserveSource>(integration);
        IntegrationRegistry.Add<IKindForceCullSource>(integration);
        IntegrationRegistry.Add<IExternalForceCullSource>(integration);
    }
}
