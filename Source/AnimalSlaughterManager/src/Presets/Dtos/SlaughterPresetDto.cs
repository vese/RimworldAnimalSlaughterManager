using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

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
