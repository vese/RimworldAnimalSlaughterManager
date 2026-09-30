using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

    public class PresetEntry
    {
        public string name;
        public string path;
        public DateTime date;
        public PresetScope scope;
        public CondBucket? condBucket;

        /// <summary>Rank for delete-gating: a context may only delete presets at or below its own rank.</summary>
        public int Rank => scope == PresetScope.All ? 3 : scope == PresetScope.Kind ? 2 : 1;

        /// <summary>True if a management context of <paramref name="contextRank"/> may delete this preset.</summary>
        public bool DeletableFrom(int contextRank) => Rank <= contextRank;
    }

    [XmlRoot("SlaughterPreset")]
    public class SlaughterPresetDto
    {
        [XmlElement("Kind")]
        public List<KindDto> Kinds = new List<KindDto>();

        public static SlaughterPresetDto From(ASM_MapComp comp)
        {
            var dto = new SlaughterPresetDto();
            foreach (var kv in comp.kindSettings)
            {
                if (kv.Value == null || !kv.Value.Customized) continue;
                dto.Kinds.Add(KindDto.From(kv.Key, kv.Value));
            }
            return dto;
        }

        public void ApplyTo(ASM_MapComp comp)
        {
            foreach (var kd in Kinds)
            {
                var def = DefDatabase<ThingDef>.GetNamed(kd.animal, false);
                if (def == null) continue;
                kd.ApplyTo(comp.GetSettings(def));
            }
        }
    }

    public class KindDto
    {
        [XmlAttribute] public string animal;
        public string malePref;
        public string femalePref;
        public string maleYoungPref;
        public string femaleYoungPref;
        [XmlArray("KeepTraits")] [XmlArrayItem("Trait")]
        public List<TraitDto> KeepTraits = new List<TraitDto>();
        [XmlArray("ForceCullTraits")] [XmlArrayItem("Trait")]
        public List<TraitDto> ForceCullTraits = new List<TraitDto>();
        [XmlArray("PrioAdultMale")] [XmlArrayItem("Cond")]
        public List<ConditionDto> PrioAdultMale;
        [XmlArray("PrioYoungMale")] [XmlArrayItem("Cond")]
        public List<ConditionDto> PrioYoungMale;
        [XmlArray("PrioAdultFemale")] [XmlArrayItem("Cond")]
        public List<ConditionDto> PrioAdultFemale;
        [XmlArray("PrioYoungFemale")] [XmlArrayItem("Cond")]
        public List<ConditionDto> PrioYoungFemale;
        [XmlArray("PrioRulesAdultMale")] [XmlArrayItem("Rule")]
        public List<PriorityRuleDto> PrioRulesAdultMale = new List<PriorityRuleDto>();
        [XmlArray("PrioRulesYoungMale")] [XmlArrayItem("Rule")]
        public List<PriorityRuleDto> PrioRulesYoungMale = new List<PriorityRuleDto>();
        [XmlArray("PrioRulesAdultFemale")] [XmlArrayItem("Rule")]
        public List<PriorityRuleDto> PrioRulesAdultFemale = new List<PriorityRuleDto>();
        [XmlArray("PrioRulesYoungFemale")] [XmlArrayItem("Rule")]
        public List<PriorityRuleDto> PrioRulesYoungFemale = new List<PriorityRuleDto>();

        public static KindDto From(ThingDef def, KindSettings k)
        {
            var dto = new KindDto { animal = def.defName };
            k.Save(dto);
            return dto;
        }

        public void ApplyTo(KindSettings ks)
        {
            ks.Reset();
            ks.Load(this);
        }
    }

    public class TraitDto
    {
        [XmlAttribute] public string trait;
        [XmlAttribute] public int keepCount = 1;
        [XmlAttribute] public string ageScope = "Both";
        [XmlAttribute] public string genderScope = "Any";
        [XmlAttribute] public string inheritMode = "Both";

        public static TraitDto From(TraitProtectRule t) => new TraitDto
        {
            trait = t.trait?.defName,
            keepCount = t.keepCount,
            ageScope = t.ageScope.ToString(),
            genderScope = t.genderScope.ToString(),
            inheritMode = t.inheritMode.ToString()
        };

        public static TraitDto From(TraitRule c) => new TraitDto
        {
            trait = c.trait?.defName,
            ageScope = c.ageScope.ToString(),
            genderScope = c.genderScope.ToString(),
            inheritMode = c.inheritMode.ToString()
        };

        public TraitProtectRule ToTarget()
        {
            var h = DefDatabase<HediffDef>.GetNamed(trait, false);
            if (h == null) return null;
            var t = new TraitProtectRule(h) { keepCount = keepCount };
            Enum.TryParse(ageScope, out t.ageScope);
            Enum.TryParse(genderScope, out t.genderScope);
            Enum.TryParse(inheritMode, out t.inheritMode);
            return t;
        }

        public TraitRule ToCull()
        {
            var h = DefDatabase<HediffDef>.GetNamed(trait, false);
            if (h == null) return null;
            var c = new TraitRule(h);
            Enum.TryParse(ageScope, out c.ageScope);
            Enum.TryParse(genderScope, out c.genderScope);
            Enum.TryParse(inheritMode, out c.inheritMode);
            return c;
        }
    }

    /// <summary>XML-serializable form of SlaughterCondition for preset files (stores defNames).</summary>
    public class ConditionDto
    {
        [XmlAttribute] public string type;
        [XmlAttribute] public bool has = true;
        [XmlAttribute] public string trait;
        [XmlAttribute] public string disease;
        [XmlAttribute] public string trainable;
        [XmlAttribute] public string inheritMode;

        public static ConditionDto From(SlaughterCondition c) => new ConditionDto
        {
            type = c.type.ToString(),
            has = c.has,
            trait = c.trait?.defName,
            disease = c.disease?.defName,
            trainable = c.trainable?.defName,
            inheritMode = c.inheritMode.ToString()
        };

        public SlaughterCondition ToCondition()
        {
            if (!Enum.TryParse(type, out CondType t)) return new SlaughterCondition(CondType.Pregnancy);
            return new SlaughterCondition(t)
            {
                has = has,
                trait = string.IsNullOrEmpty(trait) ? null : DefDatabase<HediffDef>.GetNamedSilentFail(trait),
                disease = string.IsNullOrEmpty(disease) ? null : DefDatabase<HediffDef>.GetNamedSilentFail(disease),
                trainable = string.IsNullOrEmpty(trainable) ? null : DefDatabase<TrainableDef>.GetNamedSilentFail(trainable),
                inheritMode = string.IsNullOrEmpty(inheritMode) ? TraitInheritability.Both : (TraitInheritability)Enum.Parse(typeof(TraitInheritability), inheritMode)
            };
        }
    }

    [XmlRoot("ConditionPreset")]
    public class ConditionPresetDto
    {
        [XmlAttribute] public string bucket;
        [XmlElement("Cond")] public List<ConditionDto> conditions = new List<ConditionDto>();
    }

    /// <summary>XML-serializable form of a priority-rule list preset (stores defNames).</summary>
    public class RulePresetDto
    {
        [XmlAttribute] public string bucket;
        [XmlElement("Rule")] public List<PriorityRuleDto> rules = new List<PriorityRuleDto>();
    }

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
