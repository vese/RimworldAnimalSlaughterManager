using RimWorld;
using System;
using System.Security.Policy;
using Verse;

namespace ASM;

public class TrainingGeneralPriorityRule : BasePriorityRule
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
    public override IRuleAxis? Axis => TrainingAxis.Instance;

    public override bool Covers(BasePriorityRule other) =>
        (other is TrainingGeneralPriorityRule rule && type == rule.type) ||
        // Fully trained ⊆ any training.
        (other is TrainingGeneralPriorityRule g && type == TrainingGeneralType.PartialOrFull && g.type == TrainingGeneralType.Full) ||
        // Fully trained ⇒ every skill learned.
        (other is TrainingPriorityRule t && t.has);

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
