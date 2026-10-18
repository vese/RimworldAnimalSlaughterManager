namespace ASM;

/// <summary>Implemented by a rule that can cover (fully absorb) rules of another type — possibly
/// its own: every animal matching <typeparamref name="TOther"/> also matches this rule.</summary>
public interface ICoversRule<in TOther> where TOther : BasePriorityRule
{
    bool Covers(TOther other);
}
