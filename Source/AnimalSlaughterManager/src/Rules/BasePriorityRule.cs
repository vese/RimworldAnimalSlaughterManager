using System;
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

    /// <summary>True when the rule's row in the priorities list carries extra controls
    /// (the trait rule's inheritability dropdown and the row-copy button).</summary>
    public virtual bool HasExtraControls => false;

    public abstract string Label { get; }
    public abstract BasePriorityRule Clone();
    public abstract void ExposeData();

    /// <summary>The trait set types this rule belongs to; empty for a rule that takes no part in
    /// validation. The context asks the rule for them and does the rest itself.</summary>
    public virtual IEnumerable<Type> TraitSetTypes => Type.EmptyTypes;

    /// <summary>The validators this rule needs; they may read several trait sets.</summary>
    public virtual IEnumerable<IRuleSetValidator> Validators => [];

    public abstract bool Matches(Pawn? p);

    /// <summary>True when the other rule is an identical duplicate (same type, same target and
    /// parameters). Used by DuplicateValidator; coverage is ICoversRule<TOther>.</summary>
    public virtual bool IsDuplicate(BasePriorityRule other) => false;

    public void ChangeVariant()
    {
        ChangeVariantInternal();
        SettingsChanges.Raise();
    }

    protected virtual void ChangeVariantInternal() { }
}
