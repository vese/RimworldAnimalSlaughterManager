using System.Collections.Generic;
using System.Linq;
using System.Xml;

using Verse;

namespace ASM;

/// <summary>The version-2 conversion of one kind's Settings model (KindPreset).</summary>
public class V2KindPresetConverter : IV2PresetConverter<KindPreset>
{
    private const string BucketAdultMale = "PriorityRulesAdultMale";
    private const string BucketYoungMale = "PriorityRulesYoungMale";
    private const string BucketAdultFemale = "PriorityRulesAdultFemale";
    private const string BucketYoungFemale = "PriorityRulesYoungFemale";

    private static readonly string[] BucketNames = [BucketAdultMale, BucketYoungMale, BucketAdultFemale, BucketYoungFemale];

    public PresetFile ToFile(KindPreset model, PresetScope scope, string? bucket = null) => new()
    {
        Version = PresetFormat.CurrentVersion,
        Scope = scope,
        Bucket = bucket,
        Type = nameof(V2KindPreset),
        Content = PresetSerializer.SerializeContent(ToContent(model))
    };

    public KindPreset? ToModel(PresetFile file, PresetEntry entry, ThingDef? targetDef = null) =>
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
        Elements =
        [
            RulePresetXml.Bucket(BucketAdultMale, PriorityRuleXml.ToRulePresets(model.Priorities.PriorityRulesAdultMale)),
            RulePresetXml.Bucket(BucketYoungMale, PriorityRuleXml.ToRulePresets(model.Priorities.PriorityRulesYoungMale)),
            RulePresetXml.Bucket(BucketAdultFemale, PriorityRuleXml.ToRulePresets(model.Priorities.PriorityRulesAdultFemale)),
            RulePresetXml.Bucket(BucketYoungFemale, PriorityRuleXml.ToRulePresets(model.Priorities.PriorityRulesYoungFemale)),
            .. SpecialSectionsOf(model)
        ]
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
            Priorities = new PrioritySettingsPreset()
        };

        foreach (var element in content.Elements ?? [])
        {
            var rules = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(element));

            switch (element.LocalName)
            {
                case BucketAdultMale:
                    model.Priorities.PriorityRulesAdultMale = rules;
                    break;
                case BucketYoungMale:
                    model.Priorities.PriorityRulesYoungMale = rules;
                    break;
                case BucketAdultFemale:
                    model.Priorities.PriorityRulesAdultFemale = rules;
                    break;
                case BucketYoungFemale:
                    model.Priorities.PriorityRulesYoungFemale = rules;
                    break;
            }
        }

        model.SpecialSettings = KindPresetFileSerialization.ReadIntegrations([.. (content.Elements ?? []).Where(e => !BucketNames.Contains(e.LocalName))]);

        return model;
    }

    /// <summary>The kind's rule bucket matching the age×sex key; empty when absent.</summary>
    internal static List<BasePriorityRulePreset> RulesOf(V2KindPreset content, (bool Male, bool Adult) bucket)
    {
        var name = bucket switch
        {
            (true, true) => BucketAdultMale,
            (true, false) => BucketYoungMale,
            (false, true) => BucketAdultFemale,
            _ => BucketYoungFemale
        };

        return PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket((content.Elements ?? []).FirstOrDefault(e => e.LocalName == name)));
    }

    /// <summary>The integration sections of the Settings model, serialized through their
    /// providers.</summary>
    private static List<XmlElement> SpecialSectionsOf(KindPreset model)
    {
        var holder = PresetSectionSerialization.Holder();

        foreach (var kv in model.SpecialSettings)
        {
            IntegrationServices.GetServices<IKindSettingsProvider>().FirstOrDefault(p => p.Key == kv.Key)?.WriteSection(kv.Value, holder);
        }

        return [.. PresetSectionSerialization.Children(holder)];
    }
}
