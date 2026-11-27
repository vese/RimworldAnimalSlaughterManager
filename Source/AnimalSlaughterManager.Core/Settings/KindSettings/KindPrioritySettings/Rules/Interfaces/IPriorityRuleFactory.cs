namespace ASM;

/// <summary>The factory of a rule for its preset model — registered per rule in the
/// ServiceRegistry under the preset's type; the settings resolve one by the preset at hand and
/// get the rule already filled from the preset.</summary>
public interface IPriorityRuleFactory<out TPreset> where TPreset : BasePriorityRulePreset
{
    BasePriorityRule Create(BasePriorityRulePreset preset);
}
