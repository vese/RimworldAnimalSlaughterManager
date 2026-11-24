using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>The vanilla rule types: the discriminators of the core's own rules.</summary>
public sealed class VanillaPriorityRuleTypes : IPriorityRuleTypes
{
    public IEnumerable<(string DtoName, Func<BasePriorityRule> Factory)> Types =>
    [
        ("Pregnancy", static () => new PregnancyPriorityRule()),
        ("Bond", static () => new BondPriorityRule()),
        ("Disease", static () => new DiseasePriorityRule()),
        ("DiseaseAny", static () => new DiseaseAnyPriorityRule()),
        ("Training", static () => new TrainingPriorityRule()),
        ("TrainingGeneral", static () => new TrainingGeneralPriorityRule()),
    ];
}
