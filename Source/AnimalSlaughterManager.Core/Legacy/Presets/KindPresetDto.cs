using System.Linq;
using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Xml;

namespace ASM;

/// <summary>
/// The per-kind slice of the version-1 preset: prefs, the rule buckets as raw elements (the
/// children are the attribute-form Rule elements of main) and raw integration sections.
/// Read-only — KindPresetDtoConversion.FromV1 migrates it into the current model.
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

public static class KindPresetDtoConversion
{
    /// <summary>A current Settings model from a version-1 unified slice; null for null.</summary>
    public static KindPreset? FromV1(KindPresetDto? v1)
    {
        if (v1 == null)
        {
            return null;
        }

        var kind = new KindPreset
        {
            AnimalDefName = v1.animal,
            Preferences = new PreferenceSettingsPreset
            {
                MalePref = KindPresetFileParsing.ParsePref(v1.malePref),
                FemalePref = KindPresetFileParsing.ParsePref(v1.femalePref),
                MaleYoungPref = KindPresetFileParsing.ParsePref(v1.maleYoungPref),
                FemaleYoungPref = KindPresetFileParsing.ParsePref(v1.femaleYoungPref)
            },
            Priorities = new PrioritySettingsPreset
            {
                PriorityRulesAdultMale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesAdultMale?.FirstOrDefault())),
                PriorityRulesYoungMale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesYoungMale?.FirstOrDefault())),
                PriorityRulesAdultFemale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesAdultFemale?.FirstOrDefault())),
                PriorityRulesYoungFemale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesYoungFemale?.FirstOrDefault()))
            }
        };

        kind.SpecialSettings = KindPresetFileSerialization.ReadIntegrations([.. v1.IntegrationSections]);

        return kind;
    }
}
