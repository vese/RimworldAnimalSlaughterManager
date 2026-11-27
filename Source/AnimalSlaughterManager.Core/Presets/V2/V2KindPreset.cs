using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-2 file model of one kind's slice: the def name, the preferences and
/// the rule buckets and integration sections as raw elements (the buckets are named
/// PriorityRules*, the rest are the integrations' own sections — each integration owns its
/// element's shape). The converter maps it to and from the Settings model.</summary>
public class V2KindPreset
{
    [XmlAttribute]
    public string AnimalDefName = null!;

    public V2PreferenceSettings Preferences = new();

    /// <summary>The rule buckets and the integration sections, in document order.</summary>
    [XmlAnyElement]
    public XmlElement[]? Elements;
}
