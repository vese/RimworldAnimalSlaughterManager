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
        ServiceRegistry.Add<IPriorityRuleFactory<PregnancyRulePreset>>(new PriorityRuleFactory<PregnancyPriorityRule, PregnancyRulePreset>());
        ServiceRegistry.Add<IPriorityRuleFactory<BondRulePreset>>(new PriorityRuleFactory<BondPriorityRule, BondRulePreset>());
        ServiceRegistry.Add<IPriorityRuleFactory<DiseaseRulePreset>>(new PriorityRuleFactory<DiseasePriorityRule, DiseaseRulePreset>());
        ServiceRegistry.Add<IPriorityRuleFactory<DiseaseAnyRulePreset>>(new PriorityRuleFactory<DiseaseAnyPriorityRule, DiseaseAnyRulePreset>());
        ServiceRegistry.Add<IPriorityRuleFactory<TrainingRulePreset>>(new PriorityRuleFactory<TrainingPriorityRule, TrainingRulePreset>());
        ServiceRegistry.Add<IPriorityRuleFactory<TrainingGeneralRulePreset>>(new PriorityRuleFactory<TrainingGeneralPriorityRule, TrainingGeneralRulePreset>());
    }
}
