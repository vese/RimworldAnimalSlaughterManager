using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

    [XmlRoot("ConditionPreset")]
    public class ConditionPresetDto
    {
        [XmlAttribute] public string bucket;
        [XmlElement("Cond")] public List<ConditionDto> conditions = new List<ConditionDto>();
    }
