using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// The per-kind slice of a preset: prefs, rule buckets and raw integration sections.
/// Built and read by KindSettings.ToPreset/FromPreset.
/// </summary>
[XmlRoot("KindPreset")]
public class KindPresetDto : IVersionedPresetDto
{
    [XmlAttribute]
    public int version { get; set; }

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

    /// <summary>The bucket's element with the rules' DTO children; null when absent.</summary>
    public XmlElement? BucketOf((bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => PrioRulesAdultMale?.FirstOrDefault(),
        (true, false) => PrioRulesYoungMale?.FirstOrDefault(),
        (false, true) => PrioRulesAdultFemale?.FirstOrDefault(),
        _ => PrioRulesYoungFemale?.FirstOrDefault(),
    };

    public void SetBucket((bool Male, bool Adult) bucket, XmlElement element)
    {
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

}
