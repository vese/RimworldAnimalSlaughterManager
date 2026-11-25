#pragma warning disable CS0612, CS0618

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The preset shape saved by older versions: condition lists instead of rule lists.
/// Deserializes an old file and converts it into the current format; delete together with the
/// rest of the Legacy folder when the save-compatibility horizon allows.</summary>
[Obsolete]
[XmlRoot("SlaughterPreset")]
public class LegacyPresetDto
{
    [XmlElement("Kind")]
    public List<LegacyKindDto> Kinds = new List<LegacyKindDto>();

    /// <summary>The same preset in the current format: prefs and integration sections copied,
    /// condition lists converted into rule lists.</summary>
    public PresetDto ToCurrent()
    {
        var dto = new PresetDto();

        foreach (var k in Kinds)
        {
            var kd = new KindPresetDto
            {
                animal = k.animal,
                malePref = k.malePref,
                femalePref = k.femalePref,
                maleYoungPref = k.maleYoungPref,
                femaleYoungPref = k.femaleYoungPref,
                PrioRulesAdultMale = ToBucket(nameof(KindPresetDto.PrioRulesAdultMale), k.PrioAdultMale),
                PrioRulesYoungMale = ToBucket(nameof(KindPresetDto.PrioRulesYoungMale), k.PrioYoungMale),
                PrioRulesAdultFemale = ToBucket(nameof(KindPresetDto.PrioRulesAdultFemale), k.PrioAdultFemale),
                PrioRulesYoungFemale = ToBucket(nameof(KindPresetDto.PrioRulesYoungFemale), k.PrioYoungFemale)
            };
            kd.IntegrationSections.AddRange(k.IntegrationSections);
            dto.Kinds.Add(kd);
        }

        return dto;
    }

    /// <summary>True when any kind carries legacy condition lists (identifies an old file).</summary>
    public bool HasConditions =>
        Kinds.Any(k => k.PrioAdultMale?.Count > 0 || k.PrioYoungMale?.Count > 0
            || k.PrioAdultFemale?.Count > 0 || k.PrioYoungFemale?.Count > 0);

    private static XmlElement[]? ToBucket(string name, List<ConditionDto>? conditions)
    {
        var rules = new List<BasePriorityRule>();

        if (conditions != null)
        {
            foreach (var c in conditions)
            {
                var rule = KindPrioritySettingsLegacy.Convert(c.ToCondition());

                if (rule != null)
                {
                    rules.Add(rule);
                }
            }
        }

        return rules.Count > 0 ? [RulePresetXml.Bucket(name, PriorityRuleXml.ToRulePresets(rules))] : null;
    }
}

/// <summary>The per-kind slice of an old preset: preference fields, condition lists and raw
/// integration sections.</summary>
[Obsolete]
public class LegacyKindDto
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

    [XmlAnyElement]
    public List<XmlElement> IntegrationSections = new List<XmlElement>();
}
