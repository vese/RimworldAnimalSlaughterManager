using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// Self-registration of the Animal Traits System integration: RimWorld runs every
/// [StaticConstructorOnStartup] type, so each service of the integration registers itself
/// under its contract when the foreign mod is active — the core resolves them through the
/// registry contracts only.
/// </summary>
[StaticConstructorOnStartup]
public static class AnimalTraitsSystemRegistration
{
    public const string PackageId = "luved.animaltraits";

    static AnimalTraitsSystemRegistration()
    {
        // The kind-settings provider is registered unconditionally: the settings data
        // survives ATS being disabled (the types live in this assembly).
        IntegrationRegistry.Add<IKindIntegrationSettingsProvider>(new KindTraitsSettings.Provider());

        if (!ModsConfig.IsActive(PackageId))
        {
            return;
        }

        IntegrationRegistry.Add<IRuleOptionsSource>(new TraitRuleOptions());
        IntegrationRegistry.Add<ISettingsTabSource>(new TraitSettingsTab());
        IntegrationRegistry.Add<IPawnSlaughterSignals>(new TraitSlaughterSignals());
        IntegrationRegistry.Add<IKeepReserveSource>(new TraitKeepReserve());
        IntegrationRegistry.Add<IKindForceCullSource>(new TraitKindForceCull());
        IntegrationRegistry.Add<IExternalForceCullSource>(new AtsExternalForceCull());
    }
}
