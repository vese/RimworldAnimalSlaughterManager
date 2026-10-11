using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// Self-registration of the Animal Traits System integration: RimWorld runs every
/// [StaticConstructorOnStartup] type, so the integration registers itself when its foreign
/// mod is active — the core never names its integrations, it only knows the registry contracts.
/// </summary>
[StaticConstructorOnStartup]
public static class AnimalTraitsSystemRegistration
{
    static AnimalTraitsSystemRegistration()
    {
        // The kind-settings provider is registered unconditionally: the settings data
        // survives ATS being disabled (the types live in this assembly).
        IntegrationRegistry.RegisterKindSettings(new KindTraitsSettings.Provider());

        if (ModsConfig.IsActive(AnimalTraitsIntegration.PackageId))
        {
            IntegrationRegistry.RegisterTraits(new AnimalTraitsIntegration());
        }
    }
}
