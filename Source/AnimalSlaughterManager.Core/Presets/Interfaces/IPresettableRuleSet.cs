namespace ASM;

/// <summary>A rule set that knows its own List-preset slot: the presets subfolder it saves
/// into.</summary>
public interface IPresettableRuleSet
{
    string PresetsFolder { get; }
}
