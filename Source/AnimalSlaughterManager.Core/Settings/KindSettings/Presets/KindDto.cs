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

    // The rule buckets as raw elements: the rules own their DTOs, the element names of the
    // children are the type discriminators (see PriorityRuleXml).
    [XmlAnyElement("PrioRulesAdultMale")]
    public XmlElement[]? PrioRulesAdultMale;

    [XmlAnyElement("PrioRulesYoungMale")]
    public XmlElement[]? PrioRulesYoungMale;

    [XmlAnyElement("PrioRulesAdultFemale")]
    public XmlElement[]? PrioRulesAdultFemale;

    [XmlAnyElement("PrioRulesYoungFemale")]
    public XmlElement[]? PrioRulesYoungFemale;

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

    /// <summary>The bucket's element with the rules' DTO children; null when absent.</summary>
    public XmlElement? BucketOf((bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => PrioRulesAdultMale?.FirstOrDefault(),
        (true, false) => PrioRulesYoungMale?.FirstOrDefault(),
        (false, true) => PrioRulesAdultFemale?.FirstOrDefault(),
        _ => PrioRulesYoungFemale?.FirstOrDefault(),
    };

    public void SetBucket((bool Male, bool Adult) bucket, List<BasePriorityRule> rules)
    {
        var element = PriorityRuleXml.Bucket(bucket switch
        {
            (true, true) => nameof(PrioRulesAdultMale),
            (true, false) => nameof(PrioRulesYoungMale),
            (false, true) => nameof(PrioRulesAdultFemale),
            _ => nameof(PrioRulesYoungFemale),
        }, rules);

        switch (bucket)
        {
            case (true, true):
                PrioRulesAdultMale = [element];
                break;
            case (true, false):
                PrioRulesYoungMale = [element];
                break;
            case (false, true):
                PrioRulesAdultFemale = [element];
                break;
            default:
                PrioRulesYoungFemale = [element];
                break;
        }
    }

    /// <summary>Reading side of a condition preset: the export writes exactly ONE bucket list,
    /// so the single non-empty one is it — nothing is discarded. The saved bucket is the preset's
    /// origin; on apply the rules go to whatever bucket is targeted (cross-bucket import).</summary>
    public List<BasePriorityRule>? GetConditionRules()
    {
        return new[] { (male: true, adult: true), (true, false), (false, true), (false, false) }
            .Select(b => PriorityRuleXml.ReadRules(BucketOf(b)))
            .FirstOrDefault(rules => rules.Count > 0);
    }
}
