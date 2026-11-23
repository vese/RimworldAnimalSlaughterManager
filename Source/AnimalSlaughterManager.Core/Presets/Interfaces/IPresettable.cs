using System.Xml;

namespace ASM;

/// <summary>
/// A settings object that knows which of its data takes part in kind presets and how.
/// Implementations fill/read their own slice of the <see cref="KindDto"/> — the DTO itself
/// stays a plain data container.
/// </summary>
public interface IPresettable
{
    void Save(KindDto dto);

    void Load(KindDto dto);
}

/// <summary>
/// A rule set that knows its own List-preset slot: the presets subfolder, its element name in
/// a preset, and how to save/read its rules as that element.
/// </summary>
public interface IPresettableRuleSet
{
    string PresetsFolder { get; }

    /// <summary>Name of the list's element in a preset file.</summary>
    string ListElementName { get; }

    /// <summary>Appends the list's element with its rules under the parent.</summary>
    void WriteXml(XmlElement parent);

    /// <summary>Reads the rules from the list's element among the kind's children; true when
    /// the element was there.</summary>
    bool ReadXml(XmlElement kind);
}
