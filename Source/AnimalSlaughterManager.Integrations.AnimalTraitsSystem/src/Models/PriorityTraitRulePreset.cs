using System.Xml.Serialization;

namespace ASM;

/// <summary>The preset model of the concrete-trait priority rule.</summary>
[XmlRoot("Trait")]
public class PriorityTraitRulePreset
{
    [XmlAttribute]
    public bool has = true;
    [XmlAttribute]
    public string? trait;
    [XmlAttribute]
    public string inheritability = nameof(TraitInheritability.Both);
}
