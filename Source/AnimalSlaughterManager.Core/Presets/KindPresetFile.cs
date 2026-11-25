using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Linq;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The file form of a kind preset: the version plus the typed model. The buckets and
/// the integration sections serialize here (each integration owns its section's shape —
/// WriteXml/ReadXml — while this wrapper owns the element layout).</summary>
[XmlRoot("KindPreset")]
public class KindPresetFile : IVersionedPresetDto, IXmlSerializable
{
    private const string BucketAdultMale = "PrioRulesAdultMale";
    private const string BucketYoungMale = "PrioRulesYoungMale";
    private const string BucketAdultFemale = "PrioRulesAdultFemale";
    private const string BucketYoungFemale = "PrioRulesYoungFemale";

    [XmlAttribute]
    public int Version { get; set; }

    public KindPreset Kind = new();

    public XmlSchema? GetSchema() => null;

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString(nameof(Version), Version.ToString());
        writer.WriteAttributeString(nameof(KindPreset.AnimalDefName), Kind.AnimalDefName);
        writer.WriteAttributeString(nameof(PreferenceSettingsPreset.MalePref), Kind.Preferences.MalePref.ToString());
        writer.WriteAttributeString(nameof(PreferenceSettingsPreset.FemalePref), Kind.Preferences.FemalePref.ToString());
        writer.WriteAttributeString(nameof(PreferenceSettingsPreset.MaleYoungPref), Kind.Preferences.MaleYoungPref.ToString());
        writer.WriteAttributeString(nameof(PreferenceSettingsPreset.FemaleYoungPref), Kind.Preferences.FemaleYoungPref.ToString());

        KindPresetFileSerialization.SerializeBucket(writer, BucketAdultMale, Kind.Priorities.PrioRulesAdultMale);
        KindPresetFileSerialization.SerializeBucket(writer, BucketYoungMale, Kind.Priorities.PrioRulesYoungMale);
        KindPresetFileSerialization.SerializeBucket(writer, BucketAdultFemale, Kind.Priorities.PrioRulesAdultFemale);
        KindPresetFileSerialization.SerializeBucket(writer, BucketYoungFemale, Kind.Priorities.PrioRulesYoungFemale);

        var holder = PresetSectionSerialization.Holder();

        foreach (var settings in Kind.SpecialSettings.Values)
        {
            settings.WriteXml(holder);
        }

        foreach (var section in PresetSectionSerialization.Children(holder))
        {
            section.WriteTo(writer);
        }
    }

    public void ReadXml(XmlReader reader)
    {
        reader.MoveToContent();
        Version = int.TryParse(reader.GetAttribute(nameof(Version)), out var v) ? v : 0;
        Kind.AnimalDefName = reader.GetAttribute(nameof(KindPreset.AnimalDefName)) ?? null!;
        Kind.Preferences = new PreferenceSettingsPreset
        {
            MalePref = KindPresetFileParsing.ParsePref(reader.GetAttribute(nameof(PreferenceSettingsPreset.MalePref))),
            FemalePref = KindPresetFileParsing.ParsePref(reader.GetAttribute(nameof(PreferenceSettingsPreset.FemalePref))),
            MaleYoungPref = KindPresetFileParsing.ParsePref(reader.GetAttribute(nameof(PreferenceSettingsPreset.MaleYoungPref))),
            FemaleYoungPref = KindPresetFileParsing.ParsePref(reader.GetAttribute(nameof(PreferenceSettingsPreset.FemaleYoungPref)))
        };

        var doc = new XmlDocument();
        var root = (XmlElement)doc.ReadNode(reader.ReadSubtree())!;

        Kind.Priorities = new PrioritySettingsPreset
        {
            PrioRulesAdultMale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(root[BucketAdultMale])),
            PrioRulesYoungMale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(root[BucketYoungMale])),
            PrioRulesAdultFemale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(root[BucketAdultFemale])),
            PrioRulesYoungFemale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(root[BucketYoungFemale]))
        };

        Kind.SpecialSettings = KindPresetFileSerialization.ReadIntegrations(KindPresetFileSerialization.SectionsOf(root));
    }

    /// <summary>A current-format kind file from a version-1 unified slice; null for null.</summary>
    public static KindPresetFile? FromV1(KindPresetDto? v1)
    {
        if (v1 == null)
        {
            return null;
        }

        var file = new KindPresetFile
        {
            Kind = new KindPreset
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
                    PrioRulesAdultMale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesAdultMale?.FirstOrDefault())),
                    PrioRulesYoungMale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesYoungMale?.FirstOrDefault())),
                    PrioRulesAdultFemale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesAdultFemale?.FirstOrDefault())),
                    PrioRulesYoungFemale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesYoungFemale?.FirstOrDefault()))
                }
            }
        };

        file.Kind.SpecialSettings = KindPresetFileSerialization.ReadIntegrations([.. v1.IntegrationSections]);

        return file;
    }
}

public static class KindPresetFileSerialization
{
    /// <summary>Writes one bucket as an element with the rule presets inside.</summary>
    internal static void SerializeBucket(XmlWriter writer, string name, List<BasePriorityRulePreset> presets)
    {
        RulePresetXml.Bucket(name, PriorityRuleXml.ToRulePresets(presets)).WriteTo(writer);
    }

    /// <summary>The non-bucket children of a kind element (the integration sections).</summary>
    internal static List<XmlElement> SectionsOf(XmlElement root)
    {
        var sections = new List<XmlElement>();

        foreach (XmlElement child in root.ChildNodes)
        {
            if (child.LocalName is not ("PrioRulesAdultMale" or "PrioRulesYoungMale" or "PrioRulesAdultFemale" or "PrioRulesYoungFemale"))
            {
                sections.Add(child);
            }
        }

        return sections;
    }

    /// <summary>Integration settings from their sections: every provider reads the wrapped
    /// children; the ones that find their section join the slice.</summary>
    internal static Dictionary<string, IKindSettings> ReadIntegrations(List<XmlElement> sections)
    {
        var integrations = new Dictionary<string, IKindSettings>();
        var kind = PresetSectionSerialization.Wrap(sections);

        foreach (var provider in IntegrationServices.GetServices<IKindSettingsProvider>())
        {
            var settings = provider.Create();

            if (settings.ReadXml(kind))
            {
                integrations[provider.Key] = settings;
            }
        }

        return integrations;
    }
}

public static class KindPresetFileSections
{
    /// <summary>The integration sections of a kind file (serialized on demand).</summary>
    public static List<XmlElement> SectionsOf(KindPresetFile file)
    {
        var holder = PresetSectionSerialization.Holder();

        foreach (var settings in file.Kind.SpecialSettings.Values)
        {
            settings.WriteXml(holder);
        }

        return [.. PresetSectionSerialization.Children(holder)];
    }
}

public static class KindPresetFileParsing
{
    /// <summary>A pref attribute of the v1 unified slice; the default when missing.</summary>
    public static SlaughterPreference ParsePref(string? value) =>
        Enum.TryParse(value, out SlaughterPreference parsed) ? parsed : SlaughterPreference.OldestFirst;
}
