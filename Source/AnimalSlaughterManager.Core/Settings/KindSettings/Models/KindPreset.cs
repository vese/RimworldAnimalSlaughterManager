using System.Collections.Generic;
using System.Xml;

namespace ASM;

/// <summary>The per-kind slice of a preset: prefs, typed rule buckets and raw integration
/// sections. Built and read by KindSettings.ToPreset/FromPreset.</summary>
public class KindPreset
{
    public string animal = "";
    public string malePref = "";
    public string femalePref = "";
    public string maleYoungPref = "";
    public string femaleYoungPref = "";

    public List<object> PrioRulesAdultMale = [];
    public List<object> PrioRulesYoungMale = [];
    public List<object> PrioRulesAdultFemale = [];
    public List<object> PrioRulesYoungFemale = [];

    /// <summary>Raw integration sections (any unknown children); owned by their integrations.</summary>
    public List<XmlElement> IntegrationSections = [];

    public List<object> GetBucket((bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => PrioRulesAdultMale,
        (true, false) => PrioRulesYoungMale,
        (false, true) => PrioRulesAdultFemale,
        _ => PrioRulesYoungFemale,
    };

    public void SetBucket((bool Male, bool Adult) bucket, List<object> presets)
    {
        switch (bucket)
        {
            case (true, true):
                PrioRulesAdultMale = presets;
                break;
            case (true, false):
                PrioRulesYoungMale = presets;
                break;
            case (false, true):
                PrioRulesAdultFemale = presets;
                break;
            default:
                PrioRulesYoungFemale = presets;
                break;
        }
    }
}
