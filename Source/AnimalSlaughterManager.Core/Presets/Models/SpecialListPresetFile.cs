using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The file form of a special-list preset: the owning settings' own section
/// elements.</summary>
[XmlRoot("SpecialList")]
public class SpecialListPresetFile : IXmlSerializable
{
    /// <summary>The owning settings' own section elements.</summary>
    public List<XmlElement> Sections = [];

    public static SpecialListPresetFile OfSections(params IEnumerable<XmlElement>[] sections) => new() { Sections = [.. sections.SelectMany(s => s)] };

    public XmlSchema? GetSchema() => null;

    public void WriteXml(XmlWriter writer)
    {
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

        foreach (XmlElement child in root.ChildNodes)
        {
            Sections.Add(child);
        }
    }
}
