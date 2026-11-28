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
        ServiceRegistry.Add<IRuleOptionsSource>(new TrainingRuleOptionsSource());

        // The rules' factories: the preset model's factory creates its rule.
        ServiceRegistry.Add<IPriorityRuleFactory<PregnancyRulePreset>>(new PriorityRuleFactory<PregnancyRulePreset>(p => new PregnancyPriorityRule(p)));
        ServiceRegistry.Add<IPriorityRuleFactory<BondRulePreset>>(new PriorityRuleFactory<BondRulePreset>(p => new BondPriorityRule(p)));
        ServiceRegistry.Add<IPriorityRuleFactory<DiseaseRulePreset>>(new PriorityRuleFactory<DiseaseRulePreset>(p => new DiseasePriorityRule(p)));
        ServiceRegistry.Add<IPriorityRuleFactory<DiseaseAnyRulePreset>>(new PriorityRuleFactory<DiseaseAnyRulePreset>(p => new DiseaseAnyPriorityRule(p)));
        ServiceRegistry.Add<IPriorityRuleFactory<TrainingRulePreset>>(new PriorityRuleFactory<TrainingRulePreset>(p => new TrainingPriorityRule(p)));
        ServiceRegistry.Add<IPriorityRuleFactory<TrainingGeneralRulePreset>>(new PriorityRuleFactory<TrainingGeneralRulePreset>(p => new TrainingGeneralPriorityRule(p)));
    }
}
