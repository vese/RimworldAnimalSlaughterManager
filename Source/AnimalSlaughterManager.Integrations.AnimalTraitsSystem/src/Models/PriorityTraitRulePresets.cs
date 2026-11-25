using System.Xml.Serialization;

namespace ASM;

// The preset models of the ATS priority rules: the element name is the discriminator, the fields are
// the rule's own (defs as defName strings, enums as names).

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
