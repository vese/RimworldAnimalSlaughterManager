using System.Xml.Serialization;

namespace ASM;

/// <summary>The single file form of every current-format preset: the format version, the
/// metadata (the scope the preset was saved for, the content model's class name) and the
/// content model serialized as text. Reading is two plain steps — the envelope, then the
/// content by its metadata, no root-element guessing.</summary>
[XmlRoot("Preset")]
public class PresetEnvelope
{
    [XmlAttribute]
    public int Version;

    /// <summary>The scope the preset was saved for (the PresetScope value's name).</summary>
    public string Scope = "";

    /// <summary>The content model's class name.</summary>
    public string Type = "";

    /// <summary>The content model serialized to XML text.</summary>
    public string Content = "";
}
