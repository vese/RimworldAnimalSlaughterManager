using System.Collections.Generic;
using System.Linq;

using Verse;

namespace ASM;

/// <summary>The version-2 conversion of one kind's Settings model (KindPreset).</summary>
public class V2KindPresetConverter : IV2PresetConverter<KindPreset>
{
    public PresetFile ToFile(KindPreset model, PresetScope scope, string? bucket = null) => new()
    {
        Version = PresetFormat.CurrentVersion,
        Scope = scope,
        Bucket = bucket,
        Type = nameof(V2KindPreset),
        Content = PresetSerializer.SerializeContent(ToContent(model))
    };

    public KindPreset? ToModel(PresetFile file, ThingDef? targetDef = null) =>
        PresetSerializer.ReadContent<V2KindPreset>(file) is { } content ? ToModel(content) : null;

    internal static V2KindPreset ToContent(KindPreset model) => new()
    {
        AnimalDefName = model.AnimalDefName,
        Preferences = new V2PreferenceSettings
        {
            MalePref = model.Preferences.MalePref,
            FemalePref = model.Preferences.FemalePref,
            MaleYoungPref = model.Preferences.MaleYoungPref,
            FemaleYoungPref = model.Preferences.FemaleYoungPref
        },
        PriorityRulesAdultMale = ToV2(model.Priorities.PriorityRulesAdultMale),
        PriorityRulesYoungMale = ToV2(model.Priorities.PriorityRulesYoungMale),
        PriorityRulesAdultFemale = ToV2(model.Priorities.PriorityRulesAdultFemale),
        PriorityRulesYoungFemale = ToV2(model.Priorities.PriorityRulesYoungFemale),
        SpecialSettings = [.. model.SpecialSettings.Select(kv => V2SpecialSettingsMapper.ToV2(kv.Key, kv.Value)).Where(v2 => v2 != null)]
    };

    internal static KindPreset ToModel(V2KindPreset content)
    {
        var model = new KindPreset
        {
            AnimalDefName = content.AnimalDefName,
            Preferences = new PreferenceSettingsPreset
            {
                MalePref = content.Preferences.MalePref,
                FemalePref = content.Preferences.FemalePref,
                MaleYoungPref = content.Preferences.MaleYoungPref,
                FemaleYoungPref = content.Preferences.FemaleYoungPref
            },
            Priorities = new PrioritySettingsPreset
            {
                PriorityRulesAdultMale = ToSettings(content.PriorityRulesAdultMale),
                PriorityRulesYoungMale = ToSettings(content.PriorityRulesYoungMale),
                PriorityRulesAdultFemale = ToSettings(content.PriorityRulesAdultFemale),
                PriorityRulesYoungFemale = ToSettings(content.PriorityRulesYoungFemale)
            }
        };

        foreach (var v2 in content.SpecialSettings)
        {
            var settings = V2SpecialSettingsMapper.ToSettings(v2);

            if (settings != null)
            {
                model.SpecialSettings[v2.Key] = settings;
            }
        }

        return model;
    }

    /// <summary>The kind's rule bucket matching the age×sex key; empty when absent.</summary>
    internal static List<BasePriorityRulePreset> GetRules(V2KindPreset content, (bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => ToSettings(content.PriorityRulesAdultMale),
        (true, false) => ToSettings(content.PriorityRulesYoungMale),
        (false, true) => ToSettings(content.PriorityRulesAdultFemale),
        _ => ToSettings(content.PriorityRulesYoungFemale)
    };

    private static List<V2RulePreset> ToV2(List<BasePriorityRulePreset> rules) =>
        [.. rules.Select(V2RulePresetMapper.ToV2).Where(v2 => v2 != null)];

    private static List<BasePriorityRulePreset> ToSettings(List<V2RulePreset> rules) =>
        [.. rules.Select(V2RulePresetMapper.ToDto).Where(dto => dto != null)];
}
