using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>The vanilla rule types: the discriminators of the core's own rules.</summary>
public sealed class VanillaPriorityRuleTypes : IPriorityRuleTypes
{
    public IEnumerable<(string DtoName, Func<BasePriorityRule> Factory)> Types =>
    [
        (nameof(PregnancyPriorityRule), static () => new PregnancyPriorityRule()),
        (nameof(BondPriorityRule), static () => new BondPriorityRule()),
        (nameof(DiseasePriorityRule), static () => new DiseasePriorityRule()),
        (nameof(DiseaseAnyPriorityRule), static () => new DiseaseAnyPriorityRule()),
        (nameof(TrainingPriorityRule), static () => new TrainingPriorityRule()),
        (nameof(TrainingGeneralPriorityRule), static () => new TrainingGeneralPriorityRule()),
    ];
}
