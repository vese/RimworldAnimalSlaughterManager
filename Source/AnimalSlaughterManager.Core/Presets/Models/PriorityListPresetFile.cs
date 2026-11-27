using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The file form of a priority-list preset: the typed rule presets.</summary>
[XmlRoot("PriorityList")]
public class PriorityListPresetFile : IXmlSerializable
{
    private const string RulesElement = "Rules";

    /// <summary>The priority rule presets.</summary>
    public List<BasePriorityRulePreset> Rules = [];

    public static PriorityListPresetFile OfRules(List<BasePriorityRulePreset> rules) => new() { Rules = rules };

    public XmlSchema? GetSchema() => null;

    public void WriteXml(XmlWriter writer)
    {
        RulePresetXml.Bucket(RulesElement, PriorityRuleXml.ToRulePresets(Rules)).WriteTo(writer);
    }

    public void ReadXml(XmlReader reader)
    {
        reader.MoveToContent();

        var doc = new XmlDocument();
        var root = (XmlElement)doc.ReadNode(reader.ReadSubtree())!;

        Rules = PriorityRuleXml.ToDtos(RulePresetXml.ReadBucket(root[RulesElement]));
    }
}
