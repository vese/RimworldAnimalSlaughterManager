namespace ASM;

/// <summary>The version-2 file model of the disease priority rule.</summary>
[System.Xml.Serialization.XmlRoot("Disease")]
public class V2DiseaseRulePreset : V2RulePreset
{
    public bool Has;

    public string? Disease;
}
