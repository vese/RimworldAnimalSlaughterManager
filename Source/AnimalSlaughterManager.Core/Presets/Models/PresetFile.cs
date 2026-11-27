using System.Xml.Serialization;

namespace ASM;

/// <summary>The single file form of every current-format preset: the format version, the
/// metadata (the scope the preset was saved for, the content model's class name) and the
/// content model serialized as text. Reading is two plain steps — this wrapper, then the
/// content by its metadata, no root-element guessing.</summary>
[XmlRoot("Preset")]
public class PresetFile
{
    [XmlAttribute]
    public int Version;

    /// <summary>The scope the preset was saved for.</summary>
    public PresetScope Scope;

    /// <summary>The list bucket the preset belongs to (a priority condition bucket or a
    /// special-list kind); null outside the list scopes.</summary>
    public string? Bucket;

    /// <summary>The content model's class name.</summary>
    public string Type = null!;

    /// <summary>The content model serialized to XML text.</summary>
    public string Content = null!;
}
