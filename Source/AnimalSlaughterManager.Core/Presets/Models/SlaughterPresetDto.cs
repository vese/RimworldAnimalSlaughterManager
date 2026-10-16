using System.Collections.Generic;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

[XmlRoot("SlaughterPreset")]
public class SlaughterPresetDto
{
    [XmlElement("Kind")]
    public List<KindDto> Kinds = new List<KindDto>();

    public static SlaughterPresetDto From(IEnumerable<KeyValuePair<ThingDef, KindSettings>> kindSettings)
    {
        var dto = new SlaughterPresetDto();

        foreach (var kv in kindSettings)
        {
            if (kv.Value == null || !kv.Value.Customized)
            {
                continue;
            }

            dto.Kinds.Add(KindDto.Capture(kv.Key, kv.Value));
        }

        return dto;
    }

    public void ApplyTo(IKindSettingsHost host)
    {
        foreach (var kd in Kinds)
        {
            var def = DefDatabase<ThingDef>.GetNamed(kd.animal, false);

            if (def == null)
            {
                continue;
            }

            kd.ApplyTo(host.GetSettings(def));
        }
    }
}
