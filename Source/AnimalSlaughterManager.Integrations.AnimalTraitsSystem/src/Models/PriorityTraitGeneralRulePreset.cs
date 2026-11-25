using System.Xml.Serialization;

namespace ASM;

/// <summary>The preset model of the positive/negative-trait priority rule.</summary>
[XmlRoot("TraitGeneral")]
public class PriorityTraitGeneralRulePreset
{
    public bool Has { get; set; }
    public string Type { get; set; } = null!;
    public string Inheritability { get; set; } = null!;
}
