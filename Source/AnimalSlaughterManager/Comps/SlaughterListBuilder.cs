using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// Builds the recomputed slaughter list, replacing the vanilla
/// <see cref="AutoSlaughterManager.AnimalsToSlaughter"/> result when any customization is active.
///
/// Order: (1) breeding-stock (keep-trait) protection reserved up front — protected animals are
/// never slaughtered and count toward the cull thresholds; (2) vanilla per-config cull by limits
/// with our per-bucket ordering (condition lists, tie-breakers, age direction, PregnantMode);
/// (3) Animal Traits System force-cull; (4) force-cull traits regardless of limits — individual
/// protection and keep-trait protection both win here.
/// </summary>
public static class SlaughterListBuilder
{
    public static List<Pawn> Build(ASM_MapComp comp, AutoSlaughterManager mgr)
    {
        var slaughter = new List<Pawn>();
        var reserved = ReserveKeepTraits(comp);

        foreach (var config in mgr.configs)
        {
            if (config == null || config.animal == null || !config.AnyLimit)
            {
                continue;
            }
            comp.kindSettings.TryGetValue(config.animal, out var ks);
            CullForConfig(comp, config, ks, slaughter, reserved);
        }

        AddAtsForceCull(comp, slaughter, reserved);
        AddForceCullTraits(comp, slaughter, reserved);
        return slaughter;
    }

    // Breeding-stock (keep-trait) protection is reserved up front: these animals are never
    // slaughtered (it overrides even force-cull) AND they count toward the cull thresholds,
    // so a protected animal is part of the limit rather than kept on top of it. The selection
    // itself belongs to the traits integration.
    private static HashSet<Pawn> ReserveKeepTraits(ASM_MapComp comp)
    {
        var reserved = new HashSet<Pawn>();

        foreach (var kv in comp.kindSettings)
        {
            if (kv.Value == null)
            {
                continue;
            }

            foreach (var pawn in IntegrationRegistry.Traits.SelectKeepReserved(comp.map, kv.Key, kv.Value))
            {
                reserved.Add(pawn);
            }
        }

        return reserved;
    }

    private static void AddAtsForceCull(ASM_MapComp comp, List<Pawn> slaughter, HashSet<Pawn> reserved)
    {
        if (!IntegrationRegistry.Traits.Active)
        {
            return;
        }

        var cullSet = IntegrationRegistry.Traits.CullTraitDefNames;

        if (cullSet.Count == 0)
        {
            return;
        }

        foreach (var pawn in comp.map.mapPawns.SpawnedColonyAnimals)
        {
            if (slaughter.Contains(pawn) || !EligibleForSlaughter(comp, pawn, reserved))
            {
                continue;
            }

            if (IntegrationRegistry.Traits.ShouldCull(pawn, cullSet))
            {
                slaughter.Add(pawn);
            }
        }
    }

    // A pawn the custom pipeline may slaughter: passes vanilla's readiness check, is not
    // individually protected and is not reserved by a keep-trait breeding target.
    private static bool EligibleForSlaughter(ASM_MapComp comp, Pawn pawn, HashSet<Pawn> reserved) =>
        AutoSlaughterManager.CanAutoSlaughterNow(pawn)
        && !comp.IsProtected(pawn)
        && !reserved.Contains(pawn);

    private static void AddForceCullTraits(ASM_MapComp comp, List<Pawn> slaughter, HashSet<Pawn> reserved)
    {
        foreach (var kv in comp.kindSettings)
        {
            var ks = kv.Value;

            if (ks == null || !IntegrationRegistry.Traits.HasForceCullRules(ks))
            {
                continue;
            }

            foreach (var pawn in comp.map.mapPawns.SpawnedColonyAnimals)
            {
                if (pawn.def != kv.Key || slaughter.Contains(pawn) || !EligibleForSlaughter(comp, pawn, reserved))
                {
                    continue;
                }

                if (HasForceCullTrait(pawn, ks))
                {
                    slaughter.Add(pawn);
                }
            }
        }
    }

    private static void CullForConfig(ASM_MapComp comp, AutoSlaughterConfig config, KindSettings ks, List<Pawn> slaughter, HashSet<Pawn> reserved)
    {
        var males = new List<Pawn>();
        var malesYoung = new List<Pawn>();
        var females = new List<Pawn>();
        var femalesYoung = new List<Pawn>();
        var pregnant = new List<Pawn>();
        var all = new List<Pawn>();
        // Precompute each pawn's slaughter tie-breaker signals once (pregnancy, good/bad trait
        // counts, sickness) so the per-bucket sort doesn't re-scan hediffs per comparison.
        var traitDefs = IntegrationRegistry.Traits.KnownTraitDefs;
        var vitals = new Dictionary<Pawn, PawnVitals>();
        PregnantMode pregMode = comp.ResolvePregnantMode(config.animal, config.allowSlaughterPregnant);

        // Reserved (keep-trait-protected) animals of this kind count toward each threshold but
        // are never culled, so they stay out of the cullable buckets.
        int resM = 0, resMY = 0, resF = 0, resFY = 0;

        foreach (var pawn in reserved)
        {
            if (pawn.def != config.animal)
            {
                continue;
            }
            bool repro = pawn.ageTracker.CurLifeStage.reproductive;

            if (pawn.gender == Gender.Male) { if (repro) resM++; else resMY++; }
            else if (pawn.gender == Gender.Female) { if (repro) resF++; else resFY++; }
        }

        int reservedTotal = resM + resMY + resF + resFY;

        foreach (var pawn in comp.map.mapPawns.SpawnedColonyAnimals)
        {
            if (pawn.def != config.animal || !EligibleForSlaughter(comp, pawn, reserved))
            {
                continue;
            }
            if (!config.allowSlaughterBonded && pawn.relations.GetDirectRelationsCount(PawnRelationDefOf.Bond) > 0)
            {
                continue;
            }

            vitals[pawn] = VitalsOf(pawn, traitDefs);
            bool repro = pawn.ageTracker.CurLifeStage.reproductive;

            if (pawn.gender == Gender.Male)
            {
                (repro ? males : malesYoung).Add(pawn);
                all.Add(pawn);
            }
            else if (pawn.gender == Gender.Female)
            {
                bool isPregnant = PregnancyUtility.IsPregnantOrCarryingEgg(pawn);

                if (repro && !isPregnant) { females.Add(pawn); all.Add(pawn); }
                else if (repro && isPregnant)
                {
                    // Always: into the deferred-pregnant list (appended after the sort, culled last).
                    // Defer: into the bucket proper so they count toward the limit, but spared if culled.
                    // Never: excluded from culling entirely.
                    if (pregMode == PregnantMode.Always)
                    {
                        pregnant.Add(pawn);
                    }
                    else if (pregMode == PregnantMode.Defer) { females.Add(pawn); all.Add(pawn); }
                }
                else if (!repro) { femalesYoung.Add(pawn); all.Add(pawn); }
            }
            else { all.Add(pawn); }
        }

        SortBucket(males, PrefOf(ks, true, true), vitals, ks?.prioritySettings.Get(true, true));
        SortBucket(females, PrefOf(ks, false, true), vitals, ks?.prioritySettings.Get(false, true));
        SortBucket(malesYoung, PrefOf(ks, true, false), vitals, ks?.prioritySettings.Get(true, false));
        SortBucket(femalesYoung, PrefOf(ks, false, false), vitals, ks?.prioritySettings.Get(false, false));

        if (pregMode == PregnantMode.Always)
        {
            pregnant.SortByDescending(PregnancyUtility.PregnancyProgress);
            females.AddRange(pregnant);
            all.AddRange(pregnant);
        }

        // Cull each bucket down to (threshold − reserved in that bucket): protected animals
        // already fill part of the limit, so fewer others are kept. In Defer mode a pregnant
        // female that gets popped is deferred (kept alive, removed from the counts) rather than
        // slaughtered, so she does not take a kept slot.
        if (config.maxFemales != -1) while (females.Count > Math.Max(0, config.maxFemales - resF)) { var p = females.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && PregnancyUtility.IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
        if (config.maxFemalesYoung != -1) while (femalesYoung.Count > Math.Max(0, config.maxFemalesYoung - resFY)) { var p = femalesYoung.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && PregnancyUtility.IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
        if (config.maxMales != -1) while (males.Count > Math.Max(0, config.maxMales - resM)) { var p = males.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && PregnancyUtility.IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
        if (config.maxMalesYoung != -1) while (malesYoung.Count > Math.Max(0, config.maxMalesYoung - resMY)) { var p = malesYoung.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && PregnancyUtility.IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }

        SortBucket(all, SlaughterPreference.OldestFirst, vitals, null);

        if (config.maxTotal != -1)
            while (all.Count > Math.Max(0, config.maxTotal - reservedTotal)) { var p = all.PopFront(); if (!(pregMode == PregnantMode.Defer && PregnancyUtility.IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
    }

    /// <summary>Animals of this kind that match a breeding ("keep") trait target and are protected from slaughter.</summary>
    public static int CountKeptByTraits(ASM_MapComp comp, ThingDef def)
    {
        if (!comp.kindSettings.TryGetValue(def, out var ks) || ks == null)
        {
            return 0;
        }

        return IntegrationRegistry.Traits.CountKept(comp.map, def, ks);
    }

    /// <summary>Animals of this kind individually marked protected.</summary>
    public static int CountIndividuallyProtected(ASM_MapComp comp, ThingDef def)
    {
        int n = 0;
        foreach (var pa in comp.map.mapPawns.SpawnedColonyAnimals)
        {
            if (pa.def == def && comp.protectedPawnIDs.Contains(pa.thingIDNumber))
            {
                n++;
            }
        }
        return n;
    }

    private static SlaughterPreference PrefOf(KindSettings ks, bool male, bool adult)
    {
        if (ks == null)
        {
            return SlaughterPreference.OldestFirst;
        }
        if (male)
        {
            return adult ? ks.preferenceSettings.malePref : ks.preferenceSettings.maleYoungPref;
        }
        return adult ? ks.preferenceSettings.femalePref : ks.preferenceSettings.femaleYoungPref;
    }

    /// <summary>Sort so index 0 is culled first. Within a tier (same sex, same age category) the
    /// order is the trait-count/sickness tie-breakers, then the configured age direction.</summary>
    private static void SortBucket(List<Pawn> list, SlaughterPreference pref, Dictionary<Pawn, PawnVitals> vitals, List<BasePriorityRule> conditions)
    {
        bool useConds = conditions != null && conditions.Count > 0;
        var ranks = useConds ? new Dictionary<Pawn, float>() : null;
        if (useConds)
        {
            foreach (var p in list)
            {
                ranks[p] = ConditionRank(p, conditions);
            }
        }
        list.Sort((a, b) =>
        {
            // Per-bucket condition list: top = keep, bottom = cull. Lower match-index = kept;
            // unmatched lands in the middle band (count/2). Higher rank is culled first (front).
            if (useConds)
            {
                float ra = ranks[a], rb = ranks[b];
                if (ra != rb)
                {
                    return rb.CompareTo(ra);
                }
            }
            // Within the same sex and age category (this bucket), prefer to cull those with
            // fewer good traits, those with bad traits, and the sick ones. (Pregnancy is handled
            // by the per-kind PregnantMode, not as a sort key.)
            var va = vitals[a];
            var vb = vitals[b];
            int c = va.posTraits.CompareTo(vb.posTraits);                  // fewer good traits first
            if (c != 0)
            {
                return c;
            }
            c = vb.negTraits.CompareTo(va.negTraits);                  // more bad traits first
            if (c != 0)
            {
                return c;
            }
            c = (vb.sick ? 1 : 0).CompareTo(va.sick ? 1 : 0);          // sick first
            if (c != 0)
            {
                return c;
            }
            // Final decider within the category: the configured age direction.
            long aa = a.ageTracker.AgeBiologicalTicks;
            long ab = b.ageTracker.AgeBiologicalTicks;
            return pref == SlaughterPreference.YoungestFirst ? aa.CompareTo(ab) : ab.CompareTo(aa);
        });
    }

    // First matched condition index (top = keep), or the middle band (count/2) if none match.
    private static float ConditionRank(Pawn p, List<BasePriorityRule> conditions)
    {
        for (int i = 0; i < conditions.Count; i++)
        {
            if (conditions[i].Matches(p))
            {
                return i;
            }
        }
        return conditions.Count / 2f;
    }

    private struct PawnVitals
    {
        public bool pregnant;
        public int posTraits;
        public int negTraits;
        public bool sick;
    }

    // Slaughter tie-breaker signals for one pawn: pregnancy, good/bad ATS-trait counts, and
    // whether it has a disease hediff (HediffDef.makesSickThought). Computed once per recompute.
    private static PawnVitals VitalsOf(Pawn p, IReadOnlyList<HediffDef> traitDefs)
    {
        var v = new PawnVitals();
        var hs = p?.health?.hediffSet;
        if (hs == null)
        {
            return v;
        }
        v.pregnant = PregnancyUtility.IsPregnantOrCarryingEgg(p);
        int pos = 0, neg = 0;
        for (int i = 0; i < traitDefs.Count; i++)
        {
            var def = traitDefs[i];
            if (IntegrationRegistry.Traits.HasTrait(p, def))
            {
                if (def.isBad)
                {
                    neg++;
                }
                else
                {
                    pos++;
                }
            }
        }
        v.posTraits = pos;
        v.negTraits = neg;
        v.sick = IsSick(hs);
        return v;
    }

    private static bool IsSick(HediffSet hs)
    {
        var hediffs = hs.hediffs;
        if (hediffs == null)
        {
            return false;
        }
        for (int i = 0; i < hediffs.Count; i++)
        {
            var h = hediffs[i];
            if (h?.def != null && h.def.makesSickThought)
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasForceCullTrait(Pawn p, KindSettings ks) => IntegrationRegistry.Traits.ShouldForceCullBySettings(p, ks);
}
