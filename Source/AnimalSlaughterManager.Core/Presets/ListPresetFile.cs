using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The file form of a list preset: the version plus either typed rule presets (the
/// priority lists) or the owning settings' own section (the special lists).</summary>
[XmlRoot("ListPreset")]
public class ListPresetFile : IVersionedPresetDto, IXmlSerializable
{
    private const string RulesElement = "Rules";

    public int Version { get; set; }

    /// <summary>The priority rule presets; empty for a section-based list.</summary>
    public List<object> Rules = [];

    /// <summary>The owning settings' own section elements; empty for a rule list.</summary>
    public List<XmlElement> Sections = [];

    public static ListPresetFile OfRules(RuleListPreset preset) => new() { Rules = preset.Rules };

    public static ListPresetFile OfSections(params IEnumerable<XmlElement>[] sections) => new() { Sections = [.. sections.SelectMany(s => s)] };

    public RuleListPreset ToRuleList() => new() { Rules = Rules };

    public XmlSchema? GetSchema() => null;

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString(nameof(Version), Version.ToString());

        if (Rules.Count > 0)
        {
            RulePresetXml.Bucket(RulesElement, PriorityRuleXml.ToRulePresets(Rules)).WriteTo(writer);
        }

        foreach (var section in Sections)
        {
            section.WriteTo(writer);
        }
    }

    public void ReadXml(XmlReader reader)
    {
        reader.MoveToContent();
        Version = int.TryParse(reader.GetAttribute(nameof(Version)), out var v) ? v : 0;

        var doc = new XmlDocument();
        var root = (XmlElement)doc.ReadNode(reader.ReadSubtree())!;

        Rules = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(root[RulesElement]));

        foreach (XmlElement child in root.ChildNodes)
        {
            if (child.Name != RulesElement)
            {
                Sections.Add(child);
            }
        }
    }
}
