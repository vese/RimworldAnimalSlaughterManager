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

    public bool Matches(Pawn? p) => p != null && MatchesSignals(AnimalSignals.OfPawn(p));

    public abstract bool MatchesSignals(in AnimalSignals signals);

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
