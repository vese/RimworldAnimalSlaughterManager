using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

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
