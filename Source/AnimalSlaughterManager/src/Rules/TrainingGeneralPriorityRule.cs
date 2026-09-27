using RimWorld;
using System.Collections.Generic;
using System;
using System.Security.Policy;
using Verse;

namespace ASM;

public class TrainingGeneralPriorityRule : BasePriorityRule, ICoversRule<TrainingGeneralPriorityRule>, ICoversRule<TrainingPriorityRule>
{
    public TrainingGeneralType type = TrainingGeneralType.PartialOrFull;

    public override string Label => type switch
    {
        TrainingGeneralType.None => ASMKeys.CondTrainingNone.Translate(),
        TrainingGeneralType.Partial => ASMKeys.CondTrainingPartial.Translate(),
        TrainingGeneralType.PartialOrFull => throw new NotImplementedException(),
        TrainingGeneralType.Full => ASMKeys.CondTrainingFull.Translate(),
        _ => throw new NotImplementedException(),
    };

    public override BasePriorityRule Clone() => new TrainingGeneralPriorityRule() { type = type };

    public override bool Matches(Pawn? p) => type switch
    {
        TrainingGeneralType.None => !HasAnyTraining(p),
        TrainingGeneralType.Partial => HasAnyTraining(p) && !AllTrained(p),
        TrainingGeneralType.PartialOrFull => HasAnyTraining(p),
        TrainingGeneralType.Full => AllTrained(p),
        _ => throw new NotImplementedException(),
    };

    public override void ExposeData()
    {
        Scribe_Values.Look(ref type, "type", TrainingGeneralType.PartialOrFull);
    }

    // TODO: for PartialOrFull and Partial, Full
    public override IEnumerable<Type> TraitSetTypes
    {
        get
        {
            yield return typeof(TrainingTraitSet);
        }
    }

    public override IEnumerable<IRuleSetValidator> Validators => [new DuplicateValidator(), new CoverageValidator<TrainingGeneralPriorityRule, TrainingGeneralPriorityRule>(), new CoverageValidator<TrainingGeneralPriorityRule, TrainingPriorityRule>(), new StateCoverageValidator()];

    public override bool IsDuplicate(BasePriorityRule other) => other is TrainingGeneralPriorityRule rule && type == rule.type;

        /// Fully trained ⊆ any training.
        public bool Covers(TrainingGeneralPriorityRule other) =>
            type == other.type ||
            (type == TrainingGeneralType.PartialOrFull && other.type == TrainingGeneralType.Full);

        /// Fully trained ⇒ every skill learned.
        public bool Covers(TrainingPriorityRule other) => type == TrainingGeneralType.Full && other.has;

    protected override void ChangeVariantInternal() => type = type switch
    {
        TrainingGeneralType.None => TrainingGeneralType.Partial,
        TrainingGeneralType.Partial => TrainingGeneralType.PartialOrFull,
        TrainingGeneralType.PartialOrFull => TrainingGeneralType.Full,
        TrainingGeneralType.Full => TrainingGeneralType.None,
        _ => throw new NotImplementedException(),
    };

    private static bool HasAnyTraining(Pawn? p)
    {
        var tracker = p?.training;

        if (tracker == null)
        {
            return false;
        }

        foreach (var td in TrainableUtility.TrainableDefsInListOrder)
        {
            if (tracker.HasLearned(td))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AllTrained(Pawn? p)
    {
        var tracker = p?.training;

        if (tracker == null)
        {
            return false;
        }

        var trainability = p?.RaceProps?.trainability;

        if (trainability == null || trainability == TrainabilityDefOf.None)
        {
            return false;
        }

        var any = false;

        foreach (var td in TrainableUtility.TrainableDefsInListOrder)
        {
            if (td.requiredTrainability == null)
            {
                continue;
            }

            if (td.requiredTrainability.intelligenceOrder > trainability.intelligenceOrder)
            {
                continue;
            }

            any = true;

            if (!tracker.HasLearned(td))
            {
                return false;
            }
        }

        return any;
    }
}
