using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

[Obsolete]
public class KindPrioritySettingsLegacy
{
    public List<SlaughterCondition> prioAdultMale = [];
    public List<SlaughterCondition> prioYoungMale = [];
    public List<SlaughterCondition> prioAdultFemale = [];
    public List<SlaughterCondition> prioYoungFemale = [];
    
    public List<BasePriorityRule> PriorityAdultMale => prioAdultMale?.Select(Convert).Where(x => x != null).Select(x => x!).ToList() ?? [];
    public List<BasePriorityRule> PriorityYoungMale => prioYoungMale?.Select(Convert).Where(x => x != null).Select(x => x!).ToList() ?? [];
    public List<BasePriorityRule> PriorityAdultFemale => prioAdultFemale?.Select(Convert).Where(x => x != null).Select(x => x!).ToList() ?? [];
    public List<BasePriorityRule> PriorityYoungFemale => prioYoungFemale?.Select(Convert).Where(x => x != null).Select(x => x!).ToList() ?? [];

    public void ExposeData()
    {
        Scribe_Collections.Look(ref prioAdultMale, "prioAdultMale", LookMode.Deep);
        Scribe_Collections.Look(ref prioYoungMale, "prioYoungMale", LookMode.Deep);
        Scribe_Collections.Look(ref prioAdultFemale, "prioAdultFemale", LookMode.Deep);
        Scribe_Collections.Look(ref prioYoungFemale, "prioYoungFemale", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            prioAdultMale?.RemoveAll(c => c == null || c.HasNullDef);
            prioYoungMale?.RemoveAll(c => c == null || c.HasNullDef);
            prioAdultFemale?.RemoveAll(c => c == null || c.HasNullDef);
            prioYoungFemale?.RemoveAll(c => c == null || c.HasNullDef);
        }
    }

    public void Reset()
    {
        prioAdultMale.Clear();
        prioYoungMale.Clear();
        prioAdultFemale.Clear();
        prioYoungFemale.Clear();
    }

    public static BasePriorityRule? Convert(SlaughterCondition slaughterCondition) => slaughterCondition.type switch
    {
        CondType.Pregnancy => new PregnancyPriorityRule
        {
            has = slaughterCondition.has
        },
        CondType.Bond => new BondPriorityRule
        {
            has = slaughterCondition.has
        },
        CondType.DiseaseAny => new DiseaseAnyPriorityRule
        {
            has = slaughterCondition.has
        },
        CondType.Disease => new DiseasePriorityRule
        {
            has = slaughterCondition.has,
            disease = slaughterCondition.disease
        },
        CondType.TrainingNone => new TrainingGeneralPriorityRule
        {
            type = slaughterCondition.has ? TrainingGeneralType.None : TrainingGeneralType.PartialOrFull
        },
        CondType.TrainingPartial => new TrainingGeneralPriorityRule
        {
            type = slaughterCondition.has ? TrainingGeneralType.PartialOrFull : TrainingGeneralType.None
        },
        CondType.TrainingFull => new TrainingGeneralPriorityRule
        {
            type = slaughterCondition.has ? TrainingGeneralType.Full : TrainingGeneralType.Partial
        },
        CondType.Training => new TrainingPriorityRule
        {
            has = slaughterCondition.has,
            trainable = slaughterCondition.trainable
        },
        CondType.Trait => new TraitPriorityRule
        {
            has = slaughterCondition.has,
            trait = slaughterCondition.trait,
            inheritMode = slaughterCondition.inheritMode
        },
        CondType.HasPositiveTrait => new TraitGeneralPriorityRule
        {
            has = slaughterCondition.has,
            type = slaughterCondition.has ? TraitType.Positive : TraitType.Negative,
            inheritability = TraitInheritability.Both
        },
        CondType.HasNegativeTrait => new TraitGeneralPriorityRule
        {
            has = slaughterCondition.has,
            type = slaughterCondition.has ? TraitType.Negative : TraitType.Positive,
            inheritability = TraitInheritability.Both
        },
        _ => null,
    };
}
