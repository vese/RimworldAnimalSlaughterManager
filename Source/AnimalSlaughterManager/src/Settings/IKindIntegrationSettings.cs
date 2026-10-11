using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using Verse;

namespace ASM;

/// <summary>
/// Per-kind settings owned by an integration (e.g. the ATS keep/force-cull trait lists). The
/// core stores them opaquely under the integration's key — in the save (inside the KindSettings
/// node) and in kind presets (as the integration's own XML section) — and never names their type.
/// </summary>
public interface IKindIntegrationSettings : IExposable
{
    /// <summary>True when the integration's settings deviate from the default and the kind
    /// must be treated as customized.</summary>
    bool Customized { get; }

    void Reset();

    /// <summary>Writes the preset slice: appends the integration's section element (named by
    /// the provider's key) with its contents under <paramref name="parent"/>.</summary>
    void WriteXml(XmlElement parent);

    /// <summary>Reads the preset slice from a Kind element: the integration's own section, or
    /// the legacy flat children saved by earlier versions.</summary>
    void ReadXml(XmlElement kind);
}

/// <summary>The integration's kind-settings slot: the storage key (also the XML section name)
/// and a fresh instance. Registered unconditionally — the settings survive their mod being
/// disabled, so the type must be reachable regardless of mod activity.</summary>
public interface IKindIntegrationSettingsProvider
{
    string KindSettingsKey { get; }

    IKindIntegrationSettings CreateKindSettings();
}

/// <summary>Bridge helpers between the preset DTO (which carries integration sections as raw
/// XML) and the section-shaped elements the settings read and write.</summary>
internal static class XmlSections
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
