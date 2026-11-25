using System.Xml.Serialization;

namespace ASM;

/// <summary>The preset model of the positive/negative-trait priority rule.</summary>
[XmlRoot("TraitGeneral")]
public class PriorityTraitGeneralRulePreset
{
    public bool has { get; set; }
    public string type { get; set; } = "";
    public string inheritability { get; set; } = "";
}
