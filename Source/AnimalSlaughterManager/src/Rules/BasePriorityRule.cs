using System.Collections.Generic;
using Verse;

namespace ASM;

public abstract class BasePriorityRule : IExposable
{
    /// <summary>
    /// True when this condition references a def (trait/disease/trainable) that failed
    /// to resolve on load (def-providing mod disabled). Such entries are no-ops and get pruned.
    /// </summary>
    public virtual bool HasNullDef { get; } = false;
    public virtual bool HasExtraParameters { get; } = false;
    public abstract string Label { get; }
    public abstract BasePriorityRule Clone();
    public abstract void ExposeData();

    /// <summary>
    /// Registers this rule for validation: creates-or-updates the trait set of its axis (adding
    /// itself with its list index and any concrete data — def names, flags) and registers the
    /// validators to run after the pass. The default implementation does nothing.
    /// </summary>
    public virtual void Accumulate(RuleValidationContext context, int index) { }

    public abstract bool Matches(Pawn? p);

    /// <summary>
    /// True when every animal matching <paramref name="other"/> also matches this rule
    /// (this rule's set ⊇ other's set). Rules are evaluated top-to-down and the first match
    /// wins, so a rule covered by an earlier one can never fire.
    /// </summary>
    public virtual bool Covers(BasePriorityRule other) => false;

    public void ChangeVariant()
    {
        ChangeVariantInternal();
        SettingsChanges.Raise();
    }

    protected virtual void ChangeVariantInternal() { }
}
