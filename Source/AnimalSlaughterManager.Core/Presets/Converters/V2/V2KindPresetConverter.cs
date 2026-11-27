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
        PriorityRulesAdultMale = ToV2Rules(model.Priorities.PriorityRulesAdultMale),
        PriorityRulesYoungMale = ToV2Rules(model.Priorities.PriorityRulesYoungMale),
        PriorityRulesAdultFemale = ToV2Rules(model.Priorities.PriorityRulesAdultFemale),
        PriorityRulesYoungFemale = ToV2Rules(model.Priorities.PriorityRulesYoungFemale),
        SpecialSettings = [.. model.SpecialSettings.Select(ToV2Special).Where(v2 => v2 != null)]
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
                PriorityRulesAdultMale = ToSettingsRules(content.PriorityRulesAdultMale),
                PriorityRulesYoungMale = ToSettingsRules(content.PriorityRulesYoungMale),
                PriorityRulesAdultFemale = ToSettingsRules(content.PriorityRulesAdultFemale),
                PriorityRulesYoungFemale = ToSettingsRules(content.PriorityRulesYoungFemale)
            }
        };

        foreach (var v2 in content.SpecialSettings)
        {
            if (PresetContentSerializers.ToSettings(v2) is BaseSpecialSettingsPreset settings)
            {
                model.SpecialSettings[v2.Key] = settings;
            }
        }

        return model;
    }

    private static List<V2RulePreset> ToV2Rules(List<BasePriorityRulePreset> rules) =>
        [.. rules.Select(r => PresetContentSerializers.ToV2(r)).OfType<V2RulePreset>()];

    private static List<BasePriorityRulePreset> ToSettingsRules(List<V2RulePreset> rules) =>
        [.. rules.Select(r => PresetContentSerializers.ToSettings(r)).OfType<BasePriorityRulePreset>()];

    private static V2SpecialSettingsPreset? ToV2Special(KeyValuePair<string, BaseSpecialSettingsPreset> kv) =>
        PresetContentSerializers.ToV2(kv.Value) is V2SpecialSettingsPreset v2 ? v2.WithKey(kv.Key) : null;
}
