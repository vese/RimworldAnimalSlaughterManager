using RimWorld;
using System;
using Verse;

namespace ASM;

/// <summary>
/// One element of a per-bucket slaughter-priority list. Position encodes keep/cull priority
/// (top = keep, bottom = cull).
/// </summary>
[Obsolete]
public class SlaughterCondition : IExposable
{
    public CondType type;
    public bool has = true;
    public HediffDef? trait;
    public HediffDef? disease;
    public TrainableDef? trainable;
    public TraitInheritability inheritMode = TraitInheritability.Both;

    public SlaughterCondition() { }
    public SlaughterCondition(CondType type) { this.type = type; }

    /// <summary>True when this condition references a def (trait/disease/trainable) that failed
    /// to resolve on load (def-providing mod disabled). Such entries are no-ops and get pruned.</summary>
    public bool HasNullDef =>
        (type == CondType.Trait && trait == null) ||
        (type == CondType.Disease && disease == null) ||
        (type == CondType.Training && trainable == null);

    public void ExposeData()
    {
        Scribe_Values.Look(ref type, "type");
        Scribe_Values.Look(ref has, "has", true);
        Scribe_Defs.Look(ref trait, "trait");
        Scribe_Defs.Look(ref disease, "disease");
        Scribe_Defs.Look(ref trainable, "trainable");
        Scribe_Values.Look(ref inheritMode, "inheritMode", TraitInheritability.Both);
    }
}
