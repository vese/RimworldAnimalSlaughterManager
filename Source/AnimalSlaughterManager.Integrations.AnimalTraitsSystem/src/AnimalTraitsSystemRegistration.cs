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
        ServiceRegistry.Add<IKindSettingsProvider>(new KindTraitsSettings.Provider());

#pragma warning disable CS0618
        // Legacy save/preset conversion — goes away together with the Legacy folder.
        ServiceRegistry.Add<ILegacyConditionConverter>(new LegacyTraitConditions());
#pragma warning restore CS0618
        // The section source too: with the mod disabled the shared special-rules tab shows
        // its notice instead of the section.
        ServiceRegistry.Add<ISpecialSettingsSectionSource>(new TraitSettingsSections());

        if (!ModsConfig.IsActive(PackageId))
        {
            return;
        }

        ServiceRegistry.Add<IRuleOptionsSource>(new TraitRuleOptionsSource());
        ServiceRegistry.Add<IPawnSlaughterSignals>(new TraitSlaughterSignals());
        ServiceRegistry.Add<IKeepReserveSource>(new TraitKeepReserve());
        ServiceRegistry.Add<IKindForceCullSource>(new TraitKindForceCull());
        ServiceRegistry.Add<IExternalForceCullSource>(new AtsExternalForceCull());
    }
}
