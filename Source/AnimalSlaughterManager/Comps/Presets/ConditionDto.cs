using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

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
