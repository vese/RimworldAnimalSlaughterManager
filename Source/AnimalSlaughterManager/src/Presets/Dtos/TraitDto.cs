using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

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
