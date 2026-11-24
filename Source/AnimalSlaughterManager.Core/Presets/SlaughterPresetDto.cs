using System.Collections.Generic;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

[XmlRoot("SlaughterPreset")]
public class SlaughterPresetDto
{
    /// <summary>The carrier format version (<c>PresetFormat.CurrentVersion</c>); 0 in files
    /// saved before versioning. Stamped by PresetSerializer on save.</summary>
    [XmlAttribute]
    public int version;

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

    public void ApplyTo(ISettingsProvider host)
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
