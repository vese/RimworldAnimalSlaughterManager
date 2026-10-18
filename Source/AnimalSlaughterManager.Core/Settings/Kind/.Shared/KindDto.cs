using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// The per-kind slice of a preset: the core's own settings plus the integration sections.
/// Integration data rides as raw XML (<see cref="IntegrationSections"/>) — the DTO stays free
/// of integration types; each integration reads/writes its own section by name (and, for
/// presets saved by earlier versions, its legacy flat children).
/// </summary>
public class KindDto
{
    [XmlAttribute]
    public string animal;

    public string malePref;
    public string femalePref;
    public string maleYoungPref;
    public string femaleYoungPref;

    [XmlArray("PrioAdultMale")]
    [XmlArrayItem("Cond")]
    public List<ConditionDto> PrioAdultMale;

    [XmlArray("PrioYoungMale")]
    [XmlArrayItem("Cond")]
    public List<ConditionDto> PrioYoungMale;

    [XmlArray("PrioAdultFemale")]
    [XmlArrayItem("Cond")]
    public List<ConditionDto> PrioAdultFemale;

    [XmlArray("PrioYoungFemale")]
    [XmlArrayItem("Cond")]
    public List<ConditionDto> PrioYoungFemale;

    [XmlArray("PrioRulesAdultMale")]
    [XmlArrayItem("Rule")]
    public List<PriorityRuleDto> PrioRulesAdultMale = new List<PriorityRuleDto>();

    [XmlArray("PrioRulesYoungMale")]
    [XmlArrayItem("Rule")]
    public List<PriorityRuleDto> PrioRulesYoungMale = new List<PriorityRuleDto>();

    [XmlArray("PrioRulesAdultFemale")]
    [XmlArrayItem("Rule")]
    public List<PriorityRuleDto> PrioRulesAdultFemale = new List<PriorityRuleDto>();

    [XmlArray("PrioRulesYoungFemale")]
    [XmlArrayItem("Rule")]
    public List<PriorityRuleDto> PrioRulesYoungFemale = new List<PriorityRuleDto>();

    /// <summary>Raw integration sections (any unknown children); owned by their integrations.</summary>
    [XmlAnyElement]
    public List<XmlElement> IntegrationSections = new List<XmlElement>();

    public static KindDto Capture(ThingDef def, KindSettings k)
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

    /// <summary>The rule list a condition preset's bucket is stored in.</summary>
    public List<PriorityRuleDto> GetBucketRules((bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => PrioRulesAdultMale,
        (true, false) => PrioRulesYoungMale,
        (false, true) => PrioRulesAdultFemale,
        _ => PrioRulesYoungFemale,
    };

    /// <summary>Reading side of a condition preset: the export writes exactly ONE bucket list,
    /// so the single non-empty one is it — nothing is discarded. The saved bucket is the preset's
    /// origin; on apply the rules go to whatever bucket is targeted (cross-bucket import).</summary>
    public List<PriorityRuleDto>? GetConditionRules()
    {
        var rules = new[] { PrioRulesAdultMale, PrioRulesYoungMale, PrioRulesAdultFemale, PrioRulesYoungFemale }
            .FirstOrDefault(l => l != null && l.Count > 0);

        return rules?.ToList();
    }
}
