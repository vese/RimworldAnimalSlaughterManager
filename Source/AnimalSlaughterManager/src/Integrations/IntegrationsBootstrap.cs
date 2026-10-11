using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// The composition root of the mod integrations: at startup every integration registers its
/// services (when its foreign mod is active). This is the only place that enumerates them;
/// adding an integration means adding one line here plus its folder.
/// </summary>
[StaticConstructorOnStartup]
public static class IntegrationsBootstrap
{
    static IntegrationsBootstrap()
    {
        if (ModsConfig.IsActive(AnimalTraitsIntegration.PackageId))
        {
            IntegrationRegistry.RegisterTraits(new AnimalTraitsIntegration());
        }
    }
}
