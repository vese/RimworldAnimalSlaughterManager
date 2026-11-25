using System.Xml.Serialization;

namespace ASM;

/// <summary>The preset model of the positive/negative-trait priority rule.</summary>
[XmlRoot("TraitGeneral")]
public class PriorityTraitGeneralRulePreset
{
    [XmlAttribute]
    public bool has = true;
    [XmlAttribute]
    public string type = nameof(TraitType.Both);
    [XmlAttribute]
    public string inheritability = nameof(TraitInheritability.Both);
}
