using RimWorld;
using Verse;

namespace ASM;

public class PregnancyPriorityRule : BasePriorityRule
{
    public bool has = true;

    public override string Label => has ? ASMKeys.CondPregnantHas.Translate() : ASMKeys.CondPregnantMissing.Translate();

    public override BasePriorityRule Clone() => new PregnancyPriorityRule() { has = has };

    public override bool Matches(Pawn? p) => ASM_MapComp.IsPregnantOrCarryingEgg(p) == has;

    public override bool IsInvalid(BasePriorityRule baseRule) => baseRule is PregnancyPriorityRule;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref has, "has", true);
    }
}
