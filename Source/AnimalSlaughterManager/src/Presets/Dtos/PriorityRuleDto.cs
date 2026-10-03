using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

    /// <summary>XML form of one BasePriorityRule: the rule type name plus its parameters.</summary>
    public class PriorityRuleDto
    {
        private const string Pregnancy = nameof(PregnancyPriorityRule);
        private const string Bond = nameof(BondPriorityRule);
        private const string DiseaseAny = nameof(DiseaseAnyPriorityRule);
        private const string Disease = nameof(DiseasePriorityRule);
        private const string Training = nameof(TrainingPriorityRule);
        private const string TrainingGeneral = nameof(TrainingGeneralPriorityRule);
        private const string Trait = nameof(TraitPriorityRule);
        private const string TraitGeneral = nameof(TraitGeneralPriorityRule);

        [XmlAttribute] public string rule;
        [XmlAttribute] public bool has = true;
        [XmlAttribute] public string trait;
        [XmlAttribute] public string disease;
        [XmlAttribute] public string trainable;
        [XmlAttribute] public string generalType;
        [XmlAttribute] public string inheritability;

        public static PriorityRuleDto From(BasePriorityRule r) => r switch
        {
            PregnancyPriorityRule x => new PriorityRuleDto { rule = Pregnancy, has = x.has },
            BondPriorityRule x => new PriorityRuleDto { rule = Bond, has = x.has },
            DiseaseAnyPriorityRule x => new PriorityRuleDto { rule = DiseaseAny, has = x.has },
            DiseasePriorityRule x => new PriorityRuleDto { rule = Disease, has = x.has, disease = x.disease?.defName },
            TrainingPriorityRule x => new PriorityRuleDto { rule = Training, has = x.has, trainable = x.trainable?.defName },
            TrainingGeneralPriorityRule x => new PriorityRuleDto { rule = TrainingGeneral, generalType = x.type.ToString() },
            TraitPriorityRule x => new PriorityRuleDto { rule = Trait, has = x.has, trait = x.trait?.defName, inheritability = x.inheritability.ToString() },
            TraitGeneralPriorityRule x => new PriorityRuleDto { rule = TraitGeneral, has = x.has, generalType = x.type.ToString(), inheritability = x.inheritability.ToString() },
            _ => new PriorityRuleDto { rule = "" },
        };

        public BasePriorityRule ToRule()
        {
            switch (rule)
            {
                case Pregnancy:
                    return new PregnancyPriorityRule { has = has };
                case Bond:
                    return new BondPriorityRule { has = has };
                case DiseaseAny:
                    return new DiseaseAnyPriorityRule { has = has };
                case Disease:
                    return new DiseasePriorityRule { has = has, disease = DefDatabase<HediffDef>.GetNamedSilentFail(disease ?? "") };
                case Training:
                    return new TrainingPriorityRule { has = has, trainable = DefDatabase<TrainableDef>.GetNamedSilentFail(trainable ?? "") };
                case TrainingGeneral:
                    return Enum.TryParse(generalType, out TrainingGeneralType tg)
                        ? new TrainingGeneralPriorityRule { type = tg }
                        : null;
                case Trait:
                    return new TraitPriorityRule
                    {
                        has = has,
                        trait = DefDatabase<HediffDef>.GetNamedSilentFail(trait ?? ""),
                        inheritability = ParseInheritability(),
                    };
                case TraitGeneral:
                    return Enum.TryParse(generalType, out TraitType tt)
                        ? new TraitGeneralPriorityRule { has = has, type = tt, inheritability = ParseInheritability() }
                        : null;
                default:
                    return null;
            }
        }

        private TraitInheritability ParseInheritability() =>
            Enum.TryParse(inheritability, out TraitInheritability ti) ? ti : TraitInheritability.Both;
    }
