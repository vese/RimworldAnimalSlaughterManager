namespace ASM;

/// <summary>The version-2 file model of one trait rule item, mirroring the Settings
/// TraitDto.</summary>
public class V2TraitDto
{
    [System.Xml.Serialization.XmlAttribute]
    public string trait = null!;

    [System.Xml.Serialization.XmlAttribute]
    public int keepCount = 1;

    [System.Xml.Serialization.XmlAttribute]
    public string ageScope = nameof(AgeScope.Both);

    [System.Xml.Serialization.XmlAttribute]
    public string genderScope = "Any";

    [System.Xml.Serialization.XmlAttribute]
    public string inheritMode = nameof(TraitInheritability.Both);
}
