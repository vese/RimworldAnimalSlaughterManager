using System.Xml.Serialization;
using System.Linq;
namespace ASM;

// The preset models of the vanilla priority rules: one per rule type, plain fields
// (defs as defName strings, enums as names), no format concerns.

/// <summary>The base of the rule preset models: the derived type's element name is the type
/// discriminator in preset files.</summary>
public abstract class PriorityRulePreset
{
    public abstract string DtoName { get; }
}

[XmlRoot("Pregnancy")]
public class PregnancyRulePreset : PriorityRulePreset
{
    public override string DtoName => "Pregnancy";
    public bool has = true;
}

[XmlRoot("Bond")]
public class BondRulePreset : PriorityRulePreset
{
    public override string DtoName => "Bond";
    public bool has = true;
}

[XmlRoot("Disease")]
public class DiseaseRulePreset : PriorityRulePreset
{
    public override string DtoName => "Disease";
    public bool has = true;
    public string? disease;
}

[XmlRoot("DiseaseAny")]
public class DiseaseAnyRulePreset : PriorityRulePreset
{
    public override string DtoName => "DiseaseAny";
    public bool has = true;
}

[XmlRoot("Training")]
public class TrainingRulePreset : PriorityRulePreset
{
    public override string DtoName => "Training";
    public bool has = true;
    public string? trainable;
}

[XmlRoot("TrainingGeneral")]
public class TrainingGeneralRulePreset : PriorityRulePreset
{
    public override string DtoName => "TrainingGeneral";
    public string type = nameof(TrainingGeneralType.None);
}

/// <summary>Shared def lookup of the rule presets: the def's type is named by the owning side.</summary>
public static class RuleDtoDefs
{
    public static Verse.Def? ByName(System.Type defType, string? defName) =>
        defName == null ? null :
            Verse.GenDefDatabase.GetAllDefsInDatabaseForDef(defType).FirstOrDefault(d => d.defName == defName);
}
