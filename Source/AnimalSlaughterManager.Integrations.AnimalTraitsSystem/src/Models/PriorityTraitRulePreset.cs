using System.Xml.Serialization;

namespace ASM;

/// <summary>The preset model of the concrete-trait priority rule.</summary>
[XmlRoot("Trait")]
public class PriorityTraitRulePreset
{
    public bool Has { get; set; }
    public string? Trait { get; set; }
    public string Inheritability { get; set; } = null!;
}
