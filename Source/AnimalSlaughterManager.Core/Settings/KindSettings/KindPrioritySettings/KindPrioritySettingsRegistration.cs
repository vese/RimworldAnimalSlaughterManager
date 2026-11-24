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
        IntegrationServices.Add<IPriorityRuleTypes>(new VanillaPriorityRuleTypes());

        IntegrationServices.Add<IRuleOptionsSource>(new PregnancyRuleOptionsSource());
        IntegrationServices.Add<IRuleOptionsSource>(new BondRuleOptionsSource());
        IntegrationServices.Add<IRuleOptionsSource>(new DiseaseRuleOptionsSource());
        IntegrationServices.Add<IRuleOptionsSource>(new TrainingRuleOptionsSource());
    }
}
