namespace ASM;

/// <summary>The base of the rule preset models: the concrete type is the rule's own DTO
/// (plain fields); the file layer serializes it with XmlSerializer of the concrete type and
/// routes it by the rule class name.</summary>
public abstract class PriorityRulePreset
{
}
