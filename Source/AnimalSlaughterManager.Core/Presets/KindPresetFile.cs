using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The file form of a kind preset: the version plus the typed model, serialized by
/// hand (the rule buckets hold discriminated preset elements, the integration sections are
/// the integrations' own XML).</summary>
[XmlRoot("KindPreset")]
public class KindPresetFile : IVersionedPresetDto, IXmlSerializable
{
    [XmlAttribute("version")]
    public int Version { get; set; }

    public KindPreset Kind = new();

    public XmlSchema? GetSchema() => null;

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("version", Version.ToString());
        writer.WriteAttributeString(nameof(Kind.animal), Kind.animal);
        writer.WriteAttributeString(nameof(Kind.malePref), Kind.malePref);
        writer.WriteAttributeString(nameof(Kind.femalePref), Kind.femalePref);
        writer.WriteAttributeString(nameof(Kind.maleYoungPref), Kind.maleYoungPref);
        writer.WriteAttributeString(nameof(Kind.femaleYoungPref), Kind.femaleYoungPref);

        foreach (var (bucket, presets) in BucketsOf(Kind))
        {
            PriorityRuleXml.Bucket(bucket, presets).WriteTo(writer);
        }

        foreach (var section in Kind.IntegrationSections)
        {
            section.WriteTo(writer);
        }
    }

    public void ReadXml(XmlReader reader)
    {
        reader.MoveToContent();
        Version = int.TryParse(reader.GetAttribute("version"), out var v) ? v : 0;
        Kind.animal = reader.GetAttribute(nameof(Kind.animal)) ?? "";
        Kind.malePref = reader.GetAttribute(nameof(Kind.malePref)) ?? "";
        Kind.femalePref = reader.GetAttribute(nameof(Kind.femalePref)) ?? "";
        Kind.maleYoungPref = reader.GetAttribute(nameof(Kind.maleYoungPref)) ?? "";
        Kind.femaleYoungPref = reader.GetAttribute(nameof(Kind.femaleYoungPref)) ?? "";

        var doc = new XmlDocument();
        var root = (XmlElement)doc.ReadNode(reader.ReadSubtree())!;

        foreach (var (bucket, name) in BucketNames())
        {
            Kind.SetBucket(bucket, PriorityRuleXml.ReadPresets(root[name]));
        }

        foreach (XmlElement child in root.ChildNodes)
        {
            if (!IsBucketName(child.Name))
            {
                Kind.IntegrationSections.Add(child);
            }
        }
    }

    private static IEnumerable<(string Bucket, List<PriorityRulePreset> Presets)> BucketsOf(KindPreset kind)
    {
        yield return ("PrioRulesAdultMale", kind.PrioRulesAdultMale);
        yield return ("PrioRulesYoungMale", kind.PrioRulesYoungMale);
        yield return ("PrioRulesAdultFemale", kind.PrioRulesAdultFemale);
        yield return ("PrioRulesYoungFemale", kind.PrioRulesYoungFemale);
    }

    private static IEnumerable<((bool Male, bool Adult) Key, string Name)> BucketNames()
    {
        yield return ((true, true), "PrioRulesAdultMale");
        yield return ((true, false), "PrioRulesYoungMale");
        yield return ((false, true), "PrioRulesAdultFemale");
        yield return ((false, false), "PrioRulesYoungFemale");
    }

    private static bool IsBucketName(string name) =>
        name is "PrioRulesAdultMale" or "PrioRulesYoungMale" or "PrioRulesAdultFemale" or "PrioRulesYoungFemale";

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
                animal = v1.animal,
                malePref = v1.malePref,
                femalePref = v1.femalePref,
                maleYoungPref = v1.maleYoungPref,
                femaleYoungPref = v1.femaleYoungPref
            }
        };

        foreach (var (key, elements) in new[]
        {
            ((true, true), v1.PrioRulesAdultMale),
            ((true, false), v1.PrioRulesYoungMale),
            ((false, true), v1.PrioRulesAdultFemale),
            ((false, false), v1.PrioRulesYoungFemale)
        })
        {
            file.Kind.SetBucket(key, elements == null ? [] : PriorityRuleXml.ReadPresets(elements.FirstOrDefault()));
        }

        file.Kind.IntegrationSections.AddRange(v1.IntegrationSections);

        return file;
    }
}
