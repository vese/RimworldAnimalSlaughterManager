using RimWorld;
using Verse;

namespace ASM;

public class TrainingPriorityRule : BasePriorityRule
{
    public bool has = true;
    public TrainableDef? trainable;

    public override bool HasNullDef => trainable == null;

    public override string Label => (has ? ASMKeys.CondTrainingLearned : ASMKeys.CondTrainingNot).Translate(DefName(trainable));

    public override BasePriorityRule Clone() => new TrainingPriorityRule() { has = has, trainable = trainable };

    public override bool Matches(Pawn? p) => trainable != null && (p?.training?.HasLearned(trainable) ?? false) == has;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref has, "has", true);
        Scribe_Defs.Look(ref trainable, "trainable");
    }

    public override bool IsInvalid(BasePriorityRule baseRule) => baseRule is TrainingPriorityRule rule && trainable?.defName == rule.trainable?.defName;

    protected override void ChangeVariantInternal() => has = !has;

    private static string DefName(Def? d) => d == null ? Constants.MissingLabel : d.LabelCap.ToString();
}
