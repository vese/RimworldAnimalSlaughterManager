namespace ASM;

/// <summary>The version-2 file model of the age×sex slaughter preferences.</summary>
public class V2PreferenceSettings
{
    [System.Xml.Serialization.XmlAttribute]
    public SlaughterPreference MalePref;

    [System.Xml.Serialization.XmlAttribute]
    public SlaughterPreference FemalePref;

    [System.Xml.Serialization.XmlAttribute]
    public SlaughterPreference MaleYoungPref;

    [System.Xml.Serialization.XmlAttribute]
    public SlaughterPreference FemaleYoungPref;
}
