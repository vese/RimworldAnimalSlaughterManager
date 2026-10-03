using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

    /// <summary>XML-serializable form of a priority-rule list preset (stores defNames).</summary>
    public class RulePresetDto
    {
        [XmlAttribute] public string bucket;
        [XmlElement("Rule")] public List<PriorityRuleDto> rules = new List<PriorityRuleDto>();
    }
