using System.Linq;
using System.Xml.Serialization;
using Verse;

namespace ASM;

// The DTOs of the vanilla rules: one per rule type, the element name is the discriminator.
// Field conventions: defs as defName strings, enums as names.

[XmlRoot("Pregnancy")]
public class PregnancyRuleDto
{
    [XmlAttribute]
    public bool has = true;
}

[XmlRoot("Bond")]
public class BondRuleDto
{
    [XmlAttribute]
    public bool has = true;
}

[XmlRoot("Disease")]
public class DiseaseRuleDto
{
    [XmlAttribute]
    public bool has = true;
    [XmlAttribute]
    public string? disease;
}

[XmlRoot("DiseaseAny")]
public class DiseaseAnyRuleDto
{
    [XmlAttribute]
    public bool has = true;
}

[XmlRoot("Training")]
public class TrainingRuleDto
{
    [XmlAttribute]
    public bool has = true;
    [XmlAttribute]
    public string? trainable;
}

[XmlRoot("TrainingGeneral")]
public class TrainingGeneralRuleDto
{
    [XmlAttribute]
    public string type = nameof(TrainingGeneralType.None);
}

/// <summary>Shared def lookup of the rule DTOs: the def's type is named by the owning side.</summary>
public static class RuleDtoDefs
{
    public static Def? ByName(System.Type defType, string? defName) =>
        defName == null ? null :
        GenDefDatabase.GetAllDefsInDatabaseForDef(defType).FirstOrDefault(d => d.defName == defName);
}
