using System.Xml.Serialization;

namespace ASM;

/// <summary>The base of the version-2 file models of the integrations' special settings (one
/// subclass per integration, named "V2" + the Settings DTO's name). The Key is the provider's
/// storage key; the serializer knows every registered subtype.</summary>
public abstract class V2SpecialSettingsPreset
{
    [XmlAttribute]
    public string Key = null!;

    /// <summary>Sets the provider key (the conversion fills it after the round trip).</summary>
    public V2SpecialSettingsPreset WithKey(string key)
    {
        Key = key;
        return this;
    }
}
