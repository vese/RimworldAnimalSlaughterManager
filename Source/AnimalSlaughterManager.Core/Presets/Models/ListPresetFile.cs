using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The file form of a list preset: either typed rule presets (the priority lists) or
/// the owning settings' own section (the special lists).</summary>
[XmlRoot("ListPreset")]
public class ListPresetFile : IXmlSerializable
{
    private const string RulesElement = "Rules";

    /// <summary>The priority rule presets; empty for a section-based list.</summary>
    public List<BasePriorityRulePreset> Rules = [];

    /// <summary>The owning settings' own section elements; empty for a rule list.</summary>
    public List<XmlElement> Sections = [];

    public static ListPresetFile OfRules(List<BasePriorityRulePreset> rules) => new() { Rules = rules };

    public static ListPresetFile OfSections(params IEnumerable<XmlElement>[] sections) => new() { Sections = [.. sections.SelectMany(s => s)] };

    public List<BasePriorityRulePreset> ToRuleList() => Rules;

    public XmlSchema? GetSchema() => null;

    public void WriteXml(XmlWriter writer)
    {
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
