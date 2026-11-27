namespace ASM;

/// <summary>The version-2 file model of the training priority rule.</summary>
[System.Xml.Serialization.XmlRoot("Training")]
public class V2TrainingRulePreset : V2RulePreset
{
    public bool Has;

    public string? Trainable;
}
