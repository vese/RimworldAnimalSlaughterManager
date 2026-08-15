using System.Collections.Generic;
using Verse;

namespace ASM;

public abstract class BasePriorityRule : IExposable
{
    /// <summary>
    /// True when this condition references a def (trait/disease/trainable) that failed
    /// to resolve on load (def-providing mod disabled). Such entries are no-ops and get pruned.
    /// </summary>
    public virtual bool HasNullDef => false;
    public abstract string Label { get; }
    public abstract BasePriorityRule Clone();
    public abstract bool Matches(Pawn? p);
    public abstract void ExposeData();
    public abstract bool IsInvalid(BasePriorityRule baseRule);
}
