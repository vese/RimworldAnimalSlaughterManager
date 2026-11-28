using Verse;

namespace ASM;

/// <summary>
/// Self-registration of the core's own rule options: RimWorld runs every
/// [StaticConstructorOnStartup] type, and the vanilla rules join the add-rule menu through
/// the same contract the integrations use.
/// </summary>
[StaticConstructorOnStartup]
public static class KindPrioritySettingsRegistration
{
    static KindPrioritySettingsRegistration()
    {
        ServiceRegistry.Add<IRuleOptionsSource>(new PregnancyRuleOptionsSource());
        ServiceRegistry.Add<IRuleOptionsSource>(new BondRuleOptionsSource());
        ServiceRegistry.Add<IRuleOptionsSource>(new DiseaseRuleOptionsSource());
        ServiceRegistry.Add<IRuleOptionsSource>(new TrainingRuleOptionsSource());    }
}
