#pragma warning disable CS0612, CS0618

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-1 section plumbing: wrapping detached section elements into a kind
/// element, and the section DTO (de)serialization. Used by the Legacy readers and the
/// integrations' ReadSection seams. Delete together with the rest of the Legacy folder.</summary>
[Obsolete]
public static class PresetSectionSerialization
{
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

    /// <summary>Deserializes the object from the element (the element matches the DTO's root).</summary>
    public static T? Deserialize<T>(XmlElement element) where T : class
    {
        using var reader = new XmlNodeReader(element);
        return (T?)new XmlSerializer(typeof(T)).Deserialize(reader);
    }
}
