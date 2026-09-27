using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ASM;

public class TrainingPriorityRule : BasePriorityRule, ICoversRule<TrainingGeneralPriorityRule>
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

    private static readonly Type[] traitSetTypes = [typeof(TrainingTraitSet)];

    public override IEnumerable<Type> TraitSetTypes => traitSetTypes;

    public override IEnumerable<IRuleSetValidator> Validators => [new DuplicateValidator(), new CoverageValidator<TrainingPriorityRule, TrainingGeneralPriorityRule>(), new SetClosureValidator()];

    public override bool IsDuplicate(BasePriorityRule other) => other is TrainingPriorityRule rule && has == rule.has && trainable?.defName == rule.trainable?.defName;

        /// Untrained in a skill ⊆ no training at all.
        public bool Covers(TrainingGeneralPriorityRule other) => !has && other.type == TrainingGeneralType.None;

    protected override void ChangeVariantInternal() => has = !has;

    private static string DefName(Def? d) => d == null ? Constants.MissingLabel : d.LabelCap.ToString();
}
