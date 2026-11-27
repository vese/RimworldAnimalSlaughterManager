using System.Xml.Serialization;

namespace ASM;

/// <summary>One integration's special settings in a V2 kind slice: the provider's storage key
/// and the settings model itself (the content serializer knows every registered subtype).</summary>
public class V2SpecialSettingsEntry
{
    [XmlAttribute]
    public string Key = null!;

    public BaseSpecialSettingsPreset Settings = null!;
}
