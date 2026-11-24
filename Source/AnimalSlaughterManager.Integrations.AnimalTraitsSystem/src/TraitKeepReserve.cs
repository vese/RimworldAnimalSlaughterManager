using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>Breeding-stock reservation by the keep-trait rules: the reserved animals are never
/// slaughtered but count toward the cull thresholds. The selection biases toward keeping a
/// breeding pair for inheritable traits, then fills up to keepCount youngest-first.</summary>
public sealed class TraitKeepReserve : IKeepReserveSource
{
    public bool HasKeepRules(KindSettings ks) => ks.specialRules.Keep.HasRules;

    public IReadOnlyCollection<Pawn> SelectKeepReserved(Map map, ThingDef kind, KindSettings ks)
    {
        var reserved = new List<Pawn>();
        if (!ks.specialRules.Keep.HasRules)
        {
            return reserved;
        }

        var kindPawns = map.mapPawns.SpawnedColonyAnimals.Where(p => p.def == kind).ToList();

        foreach (var tt in ks.specialRules.Keep.rules)
        {
            if (tt?.target == null || tt.keepCount <= 0)
            {
                continue;
            }

            reserved.AddRange(SelectToKeep(kindPawns, tt));
        }

        return reserved;
    }

    public int CountKept(Map map, ThingDef kind, KindSettings ks)
    {
        int n = 0;

        foreach (var pa in map.mapPawns.SpawnedColonyAnimals)
        {
            if (pa.def != kind)
            {
                continue;
            }

            foreach (var tt in ks.specialRules.Keep.rules)
            {
                if (TraitRuleMatch.Matches(pa, tt))
                {
                    n++;
                    break;
                }
            }
        }

        return n;
    }

    private static List<Pawn> SelectToKeep(List<Pawn> kindPawns, KeepRule tt)
    {
        var keep = new List<Pawn>();

                var trait = tt.target as HediffDef;

        if (trait == null || !TraitRuleMatch.InheritableMatches(trait, TraitRuleMatch.ParseInherit(tt.inheritMode)))
        {
            return keep;
        }

        var bearers = kindPawns
            .Where(p => AnimalTraitsAccess.HasTrait(p, trait)
                && TraitRuleMatch.AgeMatches(p, tt.ageScope)
                && TraitRuleMatch.GenderMatches(p, tt.genderScope))
            .OrderBy(p => p.ageTracker.AgeBiologicalTicks)
            .ToList();

        bool biasPair = TraitRuleMatch.ParseInherit(tt.inheritMode) != TraitInheritability.NonInheritable && AnimalTraitsAccess.IsInheritable(trait);

        if (biasPair)
        {
            var male = bearers.FirstOrDefault(p => p.gender == Gender.Male && p.ageTracker.CurLifeStage.reproductive);
            var female = bearers.FirstOrDefault(p => p.gender == Gender.Female && p.ageTracker.CurLifeStage.reproductive);

            if (male != null)
            {
                keep.Add(male);
            }

            if (female != null)
            {
                keep.Add(female);
            }
        }

        foreach (var b in bearers)
        {
            if (keep.Count >= tt.keepCount)
            {
                break;
            }

            if (!keep.Contains(b))
            {
                keep.Add(b);
            }
        }

        return keep;
    }
}
