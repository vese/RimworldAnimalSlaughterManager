namespace ASM;

/// <summary>
/// Converts a legacy save/preset condition this core does not know a rule for — the
/// integration that owns the concept turns it into its own rule. Registered
/// unconditionally, so a legacy save survives its mod being disabled.
/// </summary>
public interface ILegacyConditionConverter
{
    /// <summary>The rule for the condition, or null when it is not this integration's.</summary>
    BasePriorityRule? Convert(SlaughterCondition condition);
}
