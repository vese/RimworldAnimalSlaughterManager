namespace ASM;

/// <summary>The rule's preset model — the serialized form of a rule. The inverse of the rule's
/// ToPreset: ToSetting creates the rule filled from this model.</summary>
public abstract class BasePriorityRulePreset
{
    public abstract BasePriorityRule ToSetting();
}
