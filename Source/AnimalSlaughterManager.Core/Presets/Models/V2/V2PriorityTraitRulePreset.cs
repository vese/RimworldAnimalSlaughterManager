namespace ASM;

/// <summary>The version-2 file model of the concrete-trait priority rule.</summary>
[System.Xml.Serialization.XmlRoot("Trait")]
public class V2PriorityTraitRulePreset : V2RulePreset
{
    public bool Has;

    public string? Trait;

    public string Inheritability = null!;
}
