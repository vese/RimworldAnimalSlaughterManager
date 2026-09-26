using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ASM;

public enum TrainingStatus
{
    None = 0,
    Partial = 1,
    Full = 2,
}

/// <summary>
/// The animal properties priority rules match on. Extracted from a pawn once, or synthesized by
/// the validator to explore reachability of ordered rules. Rules implement
/// <see cref="BasePriorityRule.MatchesSignals"/> over this — <c>Matches(Pawn)</c> just extracts
/// signals and delegates, so validation and runtime share one matching logic.
/// </summary>
public readonly struct AnimalSignals
{
    public readonly bool pregnant;
    public readonly bool bonded;
    public readonly bool sick;
    public readonly bool hasPositiveTrait;
    public readonly bool hasNegativeTrait;
    public readonly TrainingStatus training;
    public readonly HashSet<string> traits;
    public readonly HashSet<string> diseases;
    public readonly HashSet<string> learnedSkills;

    public AnimalSignals(bool pregnant, bool bonded, bool sick, bool hasPositiveTrait, bool hasNegativeTrait,
        TrainingStatus training, HashSet<string> traits, HashSet<string> diseases, HashSet<string> learnedSkills)
    {
        this.pregnant = pregnant;
        this.bonded = bonded;
        this.sick = sick;
        this.hasPositiveTrait = hasPositiveTrait;
        this.hasNegativeTrait = hasNegativeTrait;
        this.training = training;
        this.traits = traits;
        this.diseases = diseases;
        this.learnedSkills = learnedSkills;
    }

    public static AnimalSignals OfPawn(Pawn p)
    {
        var hediffs = p.health?.hediffSet?.hediffs;
        var diseases = new HashSet<string>();
        var traits = new HashSet<string>();
        bool sick = false;

        if (hediffs != null)
        {
            for (int i = 0; i < hediffs.Count; i++)
            {
                var h = hediffs[i];

                if (h?.def == null)
                {
                    continue;
                }

                if (h.def.makesSickThought)
                {
                    sick = true;
                    diseases.Add(h.def.defName);
                }

                if (AnimalTraitsAccess.IsAnimalTraitDef(h.def))
                {
                    traits.Add(h.def.defName);
                }
            }
        }

        bool hasPositive = false;
        bool hasNegative = false;
        var traitDefs = AnimalTraitsAccess.KnownTraitDefs;

        for (int i = 0; i < traitDefs.Count; i++)
        {
            if (traits.Contains(traitDefs[i].defName))
            {
                if (traitDefs[i].isBad)
                {
                    hasNegative = true;
                }
                else
                {
                    hasPositive = true;
                }
            }
        }

        var learned = new HashSet<string>();
        var tracker = p.training;

        if (tracker != null)
        {
            foreach (var td in TrainableUtility.TrainableDefsInListOrder)
            {
                if (tracker.HasLearned(td))
                {
                    learned.Add(td.defName);
                }
            }
        }

        var training = TrainingStatus.None;

        if (learned.Count > 0)
        {
            training = AllTrained(p) ? TrainingStatus.Full : TrainingStatus.Partial;
        }

        return new AnimalSignals(
            ASM_MapComp.IsPregnantOrCarryingEgg(p),
            (p.relations?.GetDirectRelationsCount(PawnRelationDefOf.Bond) ?? 0) > 0,
            sick,
            hasPositive,
            hasNegative,
            training,
            traits,
            diseases,
            learned);
    }

    private static bool AllTrained(Pawn p)
    {
        var tracker = p.training;

        if (tracker == null)
        {
            return false;
        }

        var trainability = p.RaceProps?.trainability;

        if (trainability == null || trainability == TrainabilityDefOf.None)
        {
            return false;
        }

        bool any = false;

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
