namespace ASM;

/// <summary>The version-2 file model of the positive/negative-trait priority rule.</summary>
[System.Xml.Serialization.XmlRoot("TraitGeneral")]
public class V2PriorityTraitGeneralRulePreset : V2RulePreset
{
    public bool Has;

    public string Type = null!;

    public string Inheritability = null!;
}
