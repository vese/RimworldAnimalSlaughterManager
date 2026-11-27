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
        PriorityRulesAdultMale = model.Priorities.PriorityRulesAdultMale,
        PriorityRulesYoungMale = model.Priorities.PriorityRulesYoungMale,
        PriorityRulesAdultFemale = model.Priorities.PriorityRulesAdultFemale,
        PriorityRulesYoungFemale = model.Priorities.PriorityRulesYoungFemale,
        SpecialSettings = [.. model.SpecialSettings.Select(kv => new V2SpecialSettingsEntry { Key = kv.Key, Settings = kv.Value })]
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
                PriorityRulesAdultMale = content.PriorityRulesAdultMale,
                PriorityRulesYoungMale = content.PriorityRulesYoungMale,
                PriorityRulesAdultFemale = content.PriorityRulesAdultFemale,
                PriorityRulesYoungFemale = content.PriorityRulesYoungFemale
            }
        };

        foreach (var entry in content.SpecialSettings)
        {
            model.SpecialSettings[entry.Key] = entry.Settings;
        }

        return model;
    }
}
