namespace ASM;

/// <summary>The base of the version-2 file models of the priority rules (one per rule, named
/// "V2" + the Settings DTO's name). The payload's serialized shape mirrors the Settings DTO's
/// today; owning it here lets the Settings models change without touching the file format.</summary>
public abstract class V2RulePreset
{
}
