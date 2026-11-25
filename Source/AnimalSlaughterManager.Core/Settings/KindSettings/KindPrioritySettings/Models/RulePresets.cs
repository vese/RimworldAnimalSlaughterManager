using System.Xml.Serialization;
using System.Linq;
namespace ASM;

// The preset models of the vanilla priority rules: one per rule type, plain fields
// (defs as defName strings, enums as names), no format concerns.

[XmlRoot("Pregnancy")]
public class PregnancyRulePreset
{
    public bool has = true;
}

[XmlRoot("Bond")]
public class BondRulePreset
{
    public bool has = true;
}

[XmlRoot("Disease")]
public class DiseaseRulePreset
{
    public bool has = true;
    public string? disease;
}

[XmlRoot("DiseaseAny")]
public class DiseaseAnyRulePreset
{
    public bool has = true;
}

[XmlRoot("Training")]
public class TrainingRulePreset
{
    public bool has = true;
    public string? trainable;
}

[XmlRoot("TrainingGeneral")]
public class TrainingGeneralRulePreset
{
    public string type = nameof(TrainingGeneralType.None);
}

/// <summary>Shared def lookup of the rule presets: the def's type is named by the owning side.</summary>
public static class RuleDtoDefs
{
    public static Verse.Def? ByName(System.Type defType, string? defName) =>
        defName == null ? null :
            Verse.GenDefDatabase.GetAllDefsInDatabaseForDef(defType).FirstOrDefault(d => d.defName == defName);
}
