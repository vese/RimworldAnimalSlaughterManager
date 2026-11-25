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
        writer.WriteAttributeString(nameof(KindPreset.Animal), Kind.Animal);
        writer.WriteAttributeString(nameof(KindPreset.MalePref), Kind.MalePref.ToString());
        writer.WriteAttributeString(nameof(KindPreset.FemalePref), Kind.FemalePref.ToString());
        writer.WriteAttributeString(nameof(KindPreset.MaleYoungPref), Kind.MaleYoungPref.ToString());
        writer.WriteAttributeString(nameof(KindPreset.FemaleYoungPref), Kind.FemaleYoungPref.ToString());

        KindPresetFileSerialization.SerializeBucket(writer, BucketAdultMale, Kind.PrioRulesAdultMale);
        KindPresetFileSerialization.SerializeBucket(writer, BucketYoungMale, Kind.PrioRulesYoungMale);
        KindPresetFileSerialization.SerializeBucket(writer, BucketAdultFemale, Kind.PrioRulesAdultFemale);
        KindPresetFileSerialization.SerializeBucket(writer, BucketYoungFemale, Kind.PrioRulesYoungFemale);

        var holder = PresetSectionSerialization.Holder();

        foreach (var settings in Kind.Integrations.Values)
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
        Kind.Animal = reader.GetAttribute(nameof(KindPreset.Animal)) ?? "";
        Enum.TryParse(reader.GetAttribute(nameof(KindPreset.MalePref)), out SlaughterPreference malePref); Kind.MalePref = malePref;
        Enum.TryParse(reader.GetAttribute(nameof(KindPreset.FemalePref)), out SlaughterPreference femalePref); Kind.FemalePref = femalePref;
        Enum.TryParse(reader.GetAttribute(nameof(KindPreset.MaleYoungPref)), out SlaughterPreference maleYoungPref); Kind.MaleYoungPref = maleYoungPref;
        Enum.TryParse(reader.GetAttribute(nameof(KindPreset.FemaleYoungPref)), out SlaughterPreference femaleYoungPref); Kind.FemaleYoungPref = femaleYoungPref;

        var doc = new XmlDocument();
        var root = (XmlElement)doc.ReadNode(reader.ReadSubtree())!;

        Kind.PrioRulesAdultMale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(root[BucketAdultMale]));
        Kind.PrioRulesYoungMale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(root[BucketYoungMale]));
        Kind.PrioRulesAdultFemale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(root[BucketAdultFemale]));
        Kind.PrioRulesYoungFemale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(root[BucketYoungFemale]));

        Kind.Integrations = KindPresetFileSerialization.ReadIntegrations(KindPresetFileSerialization.SectionsOf(root));
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
                Animal = v1.animal,
                MalePref = KindPresetFileParsing.ParsePref(v1.malePref),
                FemalePref = KindPresetFileParsing.ParsePref(v1.femalePref),
                MaleYoungPref = KindPresetFileParsing.ParsePref(v1.maleYoungPref),
                FemaleYoungPref = KindPresetFileParsing.ParsePref(v1.femaleYoungPref)
            }
        };

        file.Kind.PrioRulesAdultMale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesAdultMale?.FirstOrDefault()));
        file.Kind.PrioRulesYoungMale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesYoungMale?.FirstOrDefault()));
        file.Kind.PrioRulesAdultFemale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesAdultFemale?.FirstOrDefault()));
        file.Kind.PrioRulesYoungFemale = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(v1.PrioRulesYoungFemale?.FirstOrDefault()));

        file.Kind.Integrations = KindPresetFileSerialization.ReadIntegrations([.. v1.IntegrationSections]);

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

        foreach (var settings in file.Kind.Integrations.Values)
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
