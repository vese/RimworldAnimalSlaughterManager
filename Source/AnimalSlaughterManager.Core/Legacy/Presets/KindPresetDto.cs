using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Xml;

namespace ASM;

/// <summary>
/// The per-kind slice of the version-1 preset: prefs, the rule buckets as raw elements (the
/// children are the attribute-form Rule elements of main) and raw integration sections.
/// Read-only — KindPresetFile.FromV1 migrates it into the current model.
/// </summary>
[Obsolete]
public class KindPresetDto
{
    [XmlAttribute]
    public string animal;

    public string malePref;
    public string femalePref;
    public string maleYoungPref;
    public string femaleYoungPref;

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
}
