using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace ASM;

public static class PresetSectionSerialization
{
    /// <summary>A detached parent element to write sections into.</summary>
    public static XmlElement Holder()
    {
        return new XmlDocument().CreateElement("Sections");
    }

    /// <summary>A Kind element carrying the sections as children (imported, so from any document).</summary>
    public static XmlElement Wrap(IEnumerable<XmlElement> sections)
    {
        var doc = new XmlDocument();
        var kind = doc.CreateElement("Kind");

        foreach (var s in sections)
        {
            kind.AppendChild(doc.ImportNode(s, true));
        }

        return kind;
    }

    /// <summary>The children of the element, as elements.</summary>
    public static IEnumerable<XmlElement> Children(XmlElement parent)
    {
        foreach (var node in parent.ChildNodes)
        {
            if (node is XmlElement el)
            {
                yield return el;
            }
        }
    }

    /// <summary>Serializes the object as a child element of the parent.</summary>
    public static void Serialize(XmlElement parent, object o)
    {
        using (var writer = parent.CreateNavigator()!.AppendChild())
        {
            new XmlSerializer(o.GetType()).Serialize(writer, o);
        }
    }

    /// <summary>Deserializes the object from the element (the element matches the DTO's root).</summary>
    public static T? Deserialize<T>(XmlElement element) where T : class
    {
        using (var reader = new XmlNodeReader(element))
        {
            return (T?)new XmlSerializer(typeof(T)).Deserialize(reader);
        }
    }
}
