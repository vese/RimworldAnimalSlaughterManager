using System.Xml.Serialization;

namespace ASM;

/// <summary>The preset model of the concrete-trait priority rule.</summary>
[XmlRoot("Trait")]
public class PriorityTraitRulePreset
{
    public bool has { get; set; }
    public string? trait { get; set; }
    public string inheritability { get; set; } = "";
}
