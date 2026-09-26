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
                continue;
            comp.kindSettings.TryGetValue(config.animal, out var ks);
            CullForConfig(comp, config, ks, slaughter, reserved);
        }

        AddAtsForceCull(comp, slaughter, reserved);
        AddForceCullTraits(comp, slaughter, reserved);
        return slaughter;
    }

    // Breeding-stock (keep-trait) protection is reserved up front: these animals are never
    // slaughtered (it overrides even force-cull) AND they count toward the cull thresholds,
    // so a protected animal is part of the limit rather than kept on top of it.
    private static HashSet<Pawn> ReserveKeepTraits(ASM_MapComp comp)
    {
        var reserved = new HashSet<Pawn>();

        foreach (var kv in comp.kindSettings)
        {
            var ks = kv.Value;

            if (ks?.traitsSettings.protectRuleSet.HasRules != true)
            {
                continue;
            }

            var kindPawns = new List<Pawn>();

            foreach (var pa in comp.map.mapPawns.SpawnedColonyAnimals)
            {
                if (pa.def == kv.Key)
                {
                    kindPawns.Add(pa);
                }
            }

            foreach (var tt in ks.traitsSettings.protectRuleSet.rules)
            {
                if (tt?.trait == null || tt.keepCount <= 0)
                {
                    continue;
                }

                var keepNow = SelectToKeep(kindPawns, tt);

                foreach (var p in keepNow)
                {
                    reserved.Add(p);
                }

                if (ASMMod.Settings?.enableLogging == true)
                {
                    Log.Message($"[ASM] keep-trait kind={kv.Key.defName} trait={tt.trait.defName} filter={tt.inheritMode} traitInheritable={AnimalTraitsAccess.IsInheritable(tt.trait)} keepCount={tt.keepCount} ageScope={tt.ageScope} genderScope={tt.genderScope} reserved={keepNow.Count}");
                }
            }
        }

        return reserved;
    }

    private static void AddAtsForceCull(ASM_MapComp comp, List<Pawn> slaughter, HashSet<Pawn> reserved)
    {
        if (!AnimalTraitsAccess.IsATSActive)
        {
            return;
        }

        var cullSet = AnimalTraitsAccess.GetCullTraitDefNames();

        if (cullSet.Count == 0)
        {
            return;
        }

        foreach (var pawn in comp.map.mapPawns.SpawnedColonyAnimals)
        {
            if (slaughter.Contains(pawn)) continue;
            if (!AutoSlaughterManager.CanAutoSlaughterNow(pawn)) continue;
            if (comp.IsProtected(pawn) || reserved.Contains(pawn)) continue;

            if (AnimalTraitsAccess.ShouldCull(pawn, cullSet))
            {
                slaughter.Add(pawn);
            }
        }
    }

    private static void AddForceCullTraits(ASM_MapComp comp, List<Pawn> slaughter, HashSet<Pawn> reserved)
    {
        foreach (var kv in comp.kindSettings)
        {
            var ks = kv.Value;

            if (ks?.traitsSettings.forceCullRuleSet == null || !ks.traitsSettings.forceCullRuleSet.HasRules)
            {
                continue;
            }

            foreach (var pawn in comp.map.mapPawns.SpawnedColonyAnimals)
            {
                if (pawn.def != kv.Key) continue;
                if (slaughter.Contains(pawn)) continue;
                if (!AutoSlaughterManager.CanAutoSlaughterNow(pawn)) continue;
                if (comp.IsProtected(pawn) || reserved.Contains(pawn)) continue;

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
        var traitDefs = AnimalTraitsAccess.KnownTraitDefs;
        var vitals = new Dictionary<Pawn, PawnVitals>();
        PregnantMode pregMode = comp.ResolvePregnantMode(config.animal, config.allowSlaughterPregnant);

        // Reserved (keep-trait-protected) animals of this kind count toward each threshold but
        // are never culled, so they stay out of the cullable buckets.
        int resM = 0, resMY = 0, resF = 0, resFY = 0;

        foreach (var pawn in reserved)
        {
            if (pawn.def != config.animal) continue;
            bool repro = pawn.ageTracker.CurLifeStage.reproductive;

            if (pawn.gender == Gender.Male) { if (repro) resM++; else resMY++; }
            else if (pawn.gender == Gender.Female) { if (repro) resF++; else resFY++; }
        }

        int reservedTotal = resM + resMY + resF + resFY;

        foreach (var pawn in comp.map.mapPawns.SpawnedColonyAnimals)
        {
            if (pawn.def != config.animal) continue;
            if (!AutoSlaughterManager.CanAutoSlaughterNow(pawn)) continue;
            if (comp.IsProtected(pawn)) continue;
            if (reserved.Contains(pawn)) continue;
            if (!config.allowSlaughterBonded && pawn.relations.GetDirectRelationsCount(PawnRelationDefOf.Bond) > 0) continue;

            vitals[pawn] = VitalsOf(pawn, traitDefs);
            bool repro = pawn.ageTracker.CurLifeStage.reproductive;

            if (pawn.gender == Gender.Male)
            {
                (repro ? males : malesYoung).Add(pawn);
                all.Add(pawn);
            }
            else if (pawn.gender == Gender.Female)
            {
                bool isPregnant = PawnSlaughterInfo.IsPregnantOrCarryingEgg(pawn);

                if (repro && !isPregnant) { females.Add(pawn); all.Add(pawn); }
                else if (repro && isPregnant)
                {
                    // Always: into the deferred-pregnant list (appended after the sort, culled last).
                    // Defer: into the bucket proper so they count toward the limit, but spared if culled.
                    // Never: excluded from culling entirely.
                    if (pregMode == PregnantMode.Always) pregnant.Add(pawn);
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
            pregnant.SortByDescending(PawnSlaughterInfo.PregnancyProgress);
            females.AddRange(pregnant);
            all.AddRange(pregnant);
        }

        // Cull each bucket down to (threshold − reserved in that bucket): protected animals
        // already fill part of the limit, so fewer others are kept. In Defer mode a pregnant
        // female that gets popped is deferred (kept alive, removed from the counts) rather than
        // slaughtered, so she does not take a kept slot.
        if (config.maxFemales != -1) while (females.Count > Math.Max(0, config.maxFemales - resF)) { var p = females.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && PawnSlaughterInfo.IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
        if (config.maxFemalesYoung != -1) while (femalesYoung.Count > Math.Max(0, config.maxFemalesYoung - resFY)) { var p = femalesYoung.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && PawnSlaughterInfo.IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
        if (config.maxMales != -1) while (males.Count > Math.Max(0, config.maxMales - resM)) { var p = males.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && PawnSlaughterInfo.IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
        if (config.maxMalesYoung != -1) while (malesYoung.Count > Math.Max(0, config.maxMalesYoung - resMY)) { var p = malesYoung.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && PawnSlaughterInfo.IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }

        SortBucket(all, SlaughterPreference.OldestFirst, vitals, null);

        if (config.maxTotal != -1)
            while (all.Count > Math.Max(0, config.maxTotal - reservedTotal)) { var p = all.PopFront(); if (!(pregMode == PregnantMode.Defer && PawnSlaughterInfo.IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
    }

    /// <summary>Animals of this kind that match a breeding ("keep") trait target and are protected from slaughter.</summary>
    public static int CountKeptByTraits(ASM_MapComp comp, ThingDef def)
    {
        if (!comp.kindSettings.TryGetValue(def, out var ks) || ks?.traitsSettings.protectRuleSet == null || !ks.traitsSettings.protectRuleSet.HasRules)
            return 0;
        int n = 0;
        foreach (var pa in comp.map.mapPawns.SpawnedColonyAnimals)
        {
            if (pa.def != def) continue;
            foreach (var tt in ks.traitsSettings.protectRuleSet.rules)
            {
                if (tt.trait != null && AnimalTraitsAccess.HasTrait(pa, tt.trait)
                    && AgeMatches(pa, tt.ageScope) && GenderMatches(pa, tt.genderScope)
                    && InheritableMatches(tt.trait, tt.inheritMode))
                { n++; break; }
            }
        }
        return n;
    }

    /// <summary>Animals of this kind individually marked protected.</summary>
    public static int CountIndividuallyProtected(ASM_MapComp comp, ThingDef def)
    {
        int n = 0;
        foreach (var pa in comp.map.mapPawns.SpawnedColonyAnimals)
            if (pa.def == def && comp.protectedPawnIDs.Contains(pa.thingIDNumber)) n++;
        return n;
    }

    private static SlaughterPreference PrefOf(KindSettings ks, bool male, bool adult)
    {
        if (ks == null) return SlaughterPreference.OldestFirst;
        if (male) return adult ? ks.preferenceSettings.malePref : ks.preferenceSettings.maleYoungPref;
        return adult ? ks.preferenceSettings.femalePref : ks.preferenceSettings.femaleYoungPref;
    }

    /// <summary>Sort so index 0 is culled first. Within a tier (same sex, same age category) the
    /// order is the trait-count/sickness tie-breakers, then the configured age direction.</summary>
    private static void SortBucket(List<Pawn> list, SlaughterPreference pref, Dictionary<Pawn, PawnVitals> vitals, List<BasePriorityRule> conditions)
    {
        bool useConds = conditions != null && conditions.Count > 0;
        var ranks = useConds ? new Dictionary<Pawn, float>() : null;
        if (useConds)
            foreach (var p in list) ranks[p] = ConditionRank(p, conditions);
        list.Sort((a, b) =>
        {
            // Per-bucket condition list: top = keep, bottom = cull. Lower match-index = kept;
            // unmatched lands in the middle band (count/2). Higher rank is culled first (front).
            if (useConds)
            {
                float ra = ranks[a], rb = ranks[b];
                if (ra != rb) return rb.CompareTo(ra);
            }
            // Within the same sex and age category (this bucket), prefer to cull those with
            // fewer good traits, those with bad traits, and the sick ones. (Pregnancy is handled
            // by the per-kind PregnantMode, not as a sort key.)
            var va = vitals[a];
            var vb = vitals[b];
            int c = va.posTraits.CompareTo(vb.posTraits);                  // fewer good traits first
            if (c != 0) return c;
            c = vb.negTraits.CompareTo(va.negTraits);                  // more bad traits first
            if (c != 0) return c;
            c = (vb.sick ? 1 : 0).CompareTo(va.sick ? 1 : 0);          // sick first
            if (c != 0) return c;
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
            if (conditions[i].Matches(p)) return i;
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
        if (hs == null) return v;
        v.pregnant = PawnSlaughterInfo.IsPregnantOrCarryingEgg(p);
        int pos = 0, neg = 0;
        for (int i = 0; i < traitDefs.Count; i++)
        {
            var def = traitDefs[i];
            if (AnimalTraitsAccess.HasTrait(p, def))
            {
                if (def.isBad) neg++; else pos++;
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
        if (hediffs == null) return false;
        for (int i = 0; i < hediffs.Count; i++)
        {
            var h = hediffs[i];
            if (h?.def != null && h.def.makesSickThought) return true;
        }
        return false;
    }

    private static bool HasForceCullTrait(Pawn p, KindSettings ks)
    {
        if (ks?.traitsSettings.forceCullRuleSet == null) return false;
        foreach (var ct in ks.traitsSettings.forceCullRuleSet.rules)
            if (ct.trait != null && AnimalTraitsAccess.HasTrait(p, ct.trait)
                && AgeMatches(p, ct.ageScope) && GenderMatches(p, ct.genderScope)
                && InheritableMatches(ct.trait, ct.inheritMode))
                return true;
        return false;
    }

    public static bool AgeMatches(Pawn p, AgeScope scope)
    {
        bool repro = p.ageTracker.CurLifeStage.reproductive;
        switch (scope)
        {
            case AgeScope.Adult: return repro;
            case AgeScope.Young: return !repro;
            default: return true;
        }
    }

    public static bool GenderMatches(Pawn p, GenderScope scope)
    {
        switch (scope)
        {
            case GenderScope.Male: return p.gender == Gender.Male;
            case GenderScope.Female: return p.gender == Gender.Female;
            default: return true;
        }
    }

    public static bool InheritableMatches(HediffDef trait, TraitInheritability mode)
    {
        switch (mode)
        {
            case TraitInheritability.Inheritable: return AnimalTraitsAccess.IsInheritable(trait);
            case TraitInheritability.NonInheritable: return !AnimalTraitsAccess.IsInheritable(trait);
            default: return true;
        }
    }

    private static List<Pawn> SelectToKeep(List<Pawn> kindPawns, TraitProtectRule tt)
    {
        var keep = new List<Pawn>();
        if (!InheritableMatches(tt.trait, tt.inheritMode))
            return keep;

        var bearers = kindPawns
            .Where(p => AnimalTraitsAccess.HasTrait(p, tt.trait) && AgeMatches(p, tt.ageScope) && GenderMatches(p, tt.genderScope))
            .OrderBy(p => p.ageTracker.AgeBiologicalTicks)
            .ToList();

        bool biasPair = tt.inheritMode != TraitInheritability.NonInheritable && AnimalTraitsAccess.IsInheritable(tt.trait);
        if (biasPair)
        {
            var male = bearers.FirstOrDefault(p => p.gender == Gender.Male && p.ageTracker.CurLifeStage.reproductive);
            var female = bearers.FirstOrDefault(p => p.gender == Gender.Female && p.ageTracker.CurLifeStage.reproductive);
            if (male != null) keep.Add(male);
            if (female != null) keep.Add(female);
        }
        foreach (var b in bearers)
        {
            if (keep.Count >= tt.keepCount) break;
            if (!keep.Contains(b)) keep.Add(b);
        }
        return keep;
    }
}
