using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace ASM
{
    /// <summary>How pregnant/egg-carrying females are treated by the slaughter threshold.</summary>
    public enum PregnantMode
    {
        /// <summary>Never slaughter them; they are not counted toward the threshold.</summary>
        Never,
        /// <summary>Counted toward the threshold, but a pregnant female selected for slaughter is
        /// deferred (kept alive) until she gives birth, and does not take a kept slot.</summary>
        Defer,
        /// <summary>Slaughtered normally and counted toward the threshold.</summary>
        Always
    }

    /// <summary>
    /// Per-map settings for Animal Slaughter Manager. Owns the recomputed slaughter list that
    /// replaces the vanilla <see cref="AutoSlaughterManager.AnimalsToSlaughter"/> result when
    /// any customization is active.
    /// </summary>
    public class ASM_MapComp : MapComponent
    {
        public Dictionary<ThingDef, KindSettings> kindSettings = new Dictionary<ThingDef, KindSettings>();
        public HashSet<int> protectedPawnIDs = new HashSet<int>();
        public Dictionary<ThingDef, PregnantMode> pregnantModes = new Dictionary<ThingDef, PregnantMode>();

        // Global default age-direction prefs (General tab). Per-kind prefs override these on the Priorities tab.
        public SlaughterPreference globalMalePref = SlaughterPreference.OldestFirst;
        public SlaughterPreference globalFemalePref = SlaughterPreference.OldestFirst;
        public SlaughterPreference globalMaleYoungPref = SlaughterPreference.OldestFirst;
        public SlaughterPreference globalFemaleYoungPref = SlaughterPreference.OldestFirst;

        public bool dirty = true;
        public List<Pawn> cachedList = new List<Pawn>();

        public ASM_MapComp(Map map) : base(map) { }

        public bool AnyCustomization => protectedPawnIDs.Count > 0 || kindSettings.Values.Any(k => k.Customized) || pregnantModes.Count > 0;

        public SlaughterPreference GetGlobalPref(bool male, bool adult) =>
            male ? (adult ? globalMalePref : globalMaleYoungPref) : (adult ? globalFemalePref : globalFemaleYoungPref);

        // True if this kind has per-kind pref overrides or condition lists (differs from the global defaults).
        public bool KindHasCustomPrefs(ThingDef def)
        {
            if (!kindSettings.TryGetValue(def, out var ks) || ks == null) return false;
            return ks.malePref != globalMalePref || ks.femalePref != globalFemalePref ||
                   ks.maleYoungPref != globalMaleYoungPref || ks.femaleYoungPref != globalFemaleYoungPref ||
                   ks.AnyPrioConditions();
        }

        public KindSettings GetSettings(ThingDef def)
        {
            if (!kindSettings.TryGetValue(def, out var s))
            {
                s = new KindSettings();
                // New kinds inherit the global defaults, not hardcoded OldestFirst.
                s.malePref = globalMalePref;
                s.femalePref = globalFemalePref;
                s.maleYoungPref = globalMaleYoungPref;
                s.femaleYoungPref = globalFemaleYoungPref;
                kindSettings[def] = s;
            }
            return s;
        }

        public bool IsProtected(Pawn p) => p != null && protectedPawnIDs.Contains(p.thingIDNumber);

        // Resolve the pregnant mode for a kind: an explicit per-kind choice, otherwise fall back to
        // the vanilla allowSlaughterPregnant flag (so existing saves keep their old behavior until
        // the user picks a mode via the 3-state toggle).
        public PregnantMode ResolvePregnantMode(ThingDef def, bool allowSlaughterPregnant)
            => pregnantModes.TryGetValue(def, out var m) ? m : (allowSlaughterPregnant ? PregnantMode.Always : PregnantMode.Never);

        public void ToggleProtected(Pawn p)
        {
            if (p == null) return;
            if (!protectedPawnIDs.Add(p.thingIDNumber))
                protectedPawnIDs.Remove(p.thingIDNumber);
            dirty = true;
        }

        public void MarkDirty() => dirty = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref kindSettings, "kindSettings", LookMode.Def, LookMode.Deep);
            Scribe_Collections.Look(ref protectedPawnIDs, "protectedPawnIDs", LookMode.Value);
            Scribe_Collections.Look(ref pregnantModes, "pregnantModes", LookMode.Def, LookMode.Value);
            Scribe_Values.Look(ref globalMalePref, "globalMalePref", SlaughterPreference.OldestFirst);
            Scribe_Values.Look(ref globalFemalePref, "globalFemalePref", SlaughterPreference.OldestFirst);
            Scribe_Values.Look(ref globalMaleYoungPref, "globalMaleYoungPref", SlaughterPreference.OldestFirst);
            Scribe_Values.Look(ref globalFemaleYoungPref, "globalFemaleYoungPref", SlaughterPreference.OldestFirst);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (kindSettings == null) kindSettings = new Dictionary<ThingDef, KindSettings>();
                if (protectedPawnIDs == null) protectedPawnIDs = new HashSet<int>();
                if (pregnantModes == null) pregnantModes = new Dictionary<ThingDef, PregnantMode>();
            }
        }

        /// <summary>
        /// Recompute the slaughter list, composing with Animal Traits System:
        /// (1) vanilla per-config cull with our per-bucket age direction and cull-trait priority,
        ///     excluding protected animals and honoring ATS "spare" (via CanAutoSlaughterNow);
        /// (2) ATS force-cull candidates; (3) breeding-stock protection last (overrides ATS cull).
        /// </summary>
        public void Recompute(AutoSlaughterManager mgr)
        {
            cachedList.Clear();

            // Breeding-stock (keep-trait) protection is reserved up front: these animals are never
            // slaughtered (it overrides even force-cull) AND they count toward the cull thresholds,
            // so a protected animal is part of the limit rather than kept on top of it.
            var reserved = new HashSet<Pawn>();
            foreach (var kv in kindSettings)
            {
                var ks = kv.Value;
                if (ks?.keepTraits == null || ks.keepTraits.Count == 0) continue;
                var kindPawns = new List<Pawn>();
                foreach (var pa in map.mapPawns.SpawnedColonyAnimals)
                    if (pa.def == kv.Key) kindPawns.Add(pa);
                foreach (var tt in ks.keepTraits)
                {
                    if (tt?.trait == null || tt.keepCount <= 0) continue;
                    var keepNow = SelectToKeep(kindPawns, tt);
                    foreach (var p in keepNow)
                        reserved.Add(p);
                    if (ASMMod.Settings?.enableLogging == true)
                        Log.Message($"[ASM] keep-trait kind={kv.Key.defName} trait={tt.trait.defName} filter={tt.inheritMode} traitInheritable={AnimalTraitsAccess.IsInheritable(tt.trait)} keepCount={tt.keepCount} ageScope={tt.ageScope} genderScope={tt.genderScope} reserved={keepNow.Count}");
                }
            }

            foreach (var config in mgr.configs)
            {
                if (config == null || config.animal == null || !config.AnyLimit)
                    continue;
                kindSettings.TryGetValue(config.animal, out var ks);
                CullForConfig(config, ks, cachedList, reserved);
            }

            if (AnimalTraitsAccess.IsATSActive)
            {
                var cullSet = AnimalTraitsAccess.GetCullTraitDefNames();
                if (cullSet.Count > 0)
                {
                    foreach (var pawn in map.mapPawns.SpawnedColonyAnimals)
                    {
                        if (cachedList.Contains(pawn)) continue;
                        if (!AutoSlaughterManager.CanAutoSlaughterNow(pawn)) continue;
                        if (IsProtected(pawn) || reserved.Contains(pawn)) continue;
                        if (AnimalTraitsAccess.ShouldCull(pawn, cullSet))
                            cachedList.Add(pawn);
                    }
                }
            }

            // Force-cull traits: slaughter matching animals regardless of count/limits and other
            // settings. Individual protection and breeding-stock (keep) protection both win here.
            foreach (var kv in kindSettings)
            {
                var ks = kv.Value;
                if (ks?.forceCullTraits == null || ks.forceCullTraits.Count == 0) continue;
                foreach (var pawn in map.mapPawns.SpawnedColonyAnimals)
                {
                    if (pawn.def != kv.Key) continue;
                    if (cachedList.Contains(pawn)) continue;
                    if (!AutoSlaughterManager.CanAutoSlaughterNow(pawn)) continue;
                    if (IsProtected(pawn) || reserved.Contains(pawn)) continue;
                    if (HasForceCullTrait(pawn, ks))
                        cachedList.Add(pawn);
                }
            }
        }

        private void CullForConfig(AutoSlaughterConfig config, KindSettings ks, List<Pawn> slaughter, HashSet<Pawn> reserved)
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
            PregnantMode pregMode = ResolvePregnantMode(config.animal, config.allowSlaughterPregnant);

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

            foreach (var pawn in map.mapPawns.SpawnedColonyAnimals)
            {
                if (pawn.def != config.animal) continue;
                if (!AutoSlaughterManager.CanAutoSlaughterNow(pawn)) continue;
                if (IsProtected(pawn)) continue;
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
                    bool isPregnant = IsPregnantOrCarryingEgg(pawn);
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

            SortBucket(males, PrefOf(ks, true, true), ks, vitals, ks?.PrioOf(true, true));
            SortBucket(females, PrefOf(ks, false, true), ks, vitals, ks?.PrioOf(false, true));
            SortBucket(malesYoung, PrefOf(ks, true, false), ks, vitals, ks?.PrioOf(true, false));
            SortBucket(femalesYoung, PrefOf(ks, false, false), ks, vitals, ks?.PrioOf(false, false));
            if (pregMode == PregnantMode.Always)
            {
                pregnant.SortByDescending(p => PregnancyProgress(p));
                females.AddRange(pregnant);
                all.AddRange(pregnant);
            }

            // Cull each bucket down to (threshold − reserved in that bucket): protected animals
            // already fill part of the limit, so fewer others are kept. In Defer mode a pregnant
            // female that gets popped is deferred (kept alive, removed from the counts) rather than
            // slaughtered, so she does not take a kept slot.
            if (config.maxFemales != -1) while (females.Count > Math.Max(0, config.maxFemales - resF)) { var p = females.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
            if (config.maxFemalesYoung != -1) while (femalesYoung.Count > Math.Max(0, config.maxFemalesYoung - resFY)) { var p = femalesYoung.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
            if (config.maxMales != -1) while (males.Count > Math.Max(0, config.maxMales - resM)) { var p = males.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
            if (config.maxMalesYoung != -1) while (malesYoung.Count > Math.Max(0, config.maxMalesYoung - resMY)) { var p = malesYoung.PopFront(); all.Remove(p); if (!(pregMode == PregnantMode.Defer && IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }

            SortBucket(all, SlaughterPreference.OldestFirst, ks, vitals, null);
            if (config.maxTotal != -1)
                while (all.Count > Math.Max(0, config.maxTotal - reservedTotal)) { var p = all.PopFront(); if (!(pregMode == PregnantMode.Defer && IsPregnantOrCarryingEgg(p))) slaughter.Add(p); }
        }

        private static SlaughterPreference PrefOf(KindSettings ks, bool male, bool adult)
        {
            if (ks == null) return SlaughterPreference.OldestFirst;
            if (male) return adult ? ks.malePref : ks.maleYoungPref;
            return adult ? ks.femalePref : ks.femaleYoungPref;
        }

        /// <summary>Sort so index 0 is culled first: cull-trait carriers first, then normal,
        /// then spare-trait carriers last. Within a tier (same sex, same age category) the order is
        /// the trait-count/sickness tie-breakers, then the configured age direction.</summary>
        private static void SortBucket(List<Pawn> list, SlaughterPreference pref, KindSettings ks, Dictionary<Pawn, PawnVitals> vitals, List<SlaughterCondition> conditions)
        {
            bool useConds = conditions != null && conditions.Count > 0;
            bool useTraits = ks != null && ((ks.cullTraits != null && ks.cullTraits.Count > 0) || (ks.spareTraits != null && ks.spareTraits.Count > 0));
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
                if (useTraits)
                {
                    int sa = PriorityScore(a, ks);
                    int sb = PriorityScore(b, ks);
                    if (sa != sb) return sb.CompareTo(sa); // higher score = culled first
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
        private static float ConditionRank(Pawn p, List<SlaughterCondition> conditions)
        {
            for (int i = 0; i < conditions.Count; i++)
                if (conditions[i].Matches(p)) return i;
            return conditions.Count / 2f;
        }

        // cullTrait = 2 (first), normal = 1, spareTrait = 0 (last).
        private static int PriorityScore(Pawn p, KindSettings ks)
        {
            if (HasCullTrait(p, ks)) return 2;
            if (HasSpareTrait(p, ks)) return 0;
            return 1;
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
            v.pregnant = IsPregnantOrCarryingEgg(p);
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

        private static readonly FieldInfo EggProgressField = typeof(CompEggLayer).GetField("eggProgress", BindingFlags.NonPublic | BindingFlags.Instance);

        // "Pregnant" for slaughter purposes: a mammal with the Pregnant hediff, or an egg-layer
        // carrying a developing egg (CompEggLayer.eggProgress > 0). eggProgress is private — RimWorld
        // exposes no public accessor for an in-progress egg — so it is read via reflection.
        public static bool IsPregnantOrCarryingEgg(Pawn p)
        {
            if (p == null) return false;
            var hs = p.health?.hediffSet;
            if (hs != null && hs.HasHediff(HediffDefOf.Pregnant)) return true;
            var egg = p.TryGetComp<CompEggLayer>();
            if (egg != null && EggProgressField != null)
            {
                try { if ((float)EggProgressField.GetValue(egg) > 0f) return true; } catch { }
            }
            return false;
        }

        // Pregnancy "severity" for ordering the pregnant bucket: the Pregnant hediff severity, or for
        // egg-layers the egg progress (0..1). Falls back to 0 (also avoids NPE on egg-layers, which
        // have no Pregnant hediff).
        private static float PregnancyProgress(Pawn p)
        {
            var preg = p.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Pregnant);
            if (preg != null) return preg.Severity;
            var egg = p.TryGetComp<CompEggLayer>();
            if (egg != null && EggProgressField != null)
            {
                try { return (float)EggProgressField.GetValue(egg); } catch { }
            }
            return 0f;
        }

        private static bool HasCullTrait(Pawn p, KindSettings ks)
        {
            if (ks?.cullTraits == null) return false;
            foreach (var ct in ks.cullTraits)
                if (ct.trait != null && AnimalTraitsAccess.HasTrait(p, ct.trait)
                    && AgeMatches(p, ct.ageScope) && GenderMatches(p, ct.genderScope)
                    && InheritableMatches(ct.trait, ct.inheritMode))
                    return true;
            return false;
        }

        private static bool HasSpareTrait(Pawn p, KindSettings ks)
        {
            if (ks?.spareTraits == null) return false;
            foreach (var ct in ks.spareTraits)
                if (ct.trait != null && AnimalTraitsAccess.HasTrait(p, ct.trait)
                    && AgeMatches(p, ct.ageScope) && GenderMatches(p, ct.genderScope)
                    && InheritableMatches(ct.trait, ct.inheritMode))
                    return true;
            return false;
        }

        private static bool HasForceCullTrait(Pawn p, KindSettings ks)
        {
            if (ks?.forceCullTraits == null) return false;
            foreach (var ct in ks.forceCullTraits)
                if (ct.trait != null && AnimalTraitsAccess.HasTrait(p, ct.trait)
                    && AgeMatches(p, ct.ageScope) && GenderMatches(p, ct.genderScope)
                    && InheritableMatches(ct.trait, ct.inheritMode))
                    return true;
            return false;
        }

        private static bool AgeMatches(Pawn p, AgeScope scope)
        {
            bool repro = p.ageTracker.CurLifeStage.reproductive;
            switch (scope)
            {
                case AgeScope.Adult: return repro;
                case AgeScope.Young: return !repro;
                default: return true;
            }
        }

        private static bool GenderMatches(Pawn p, GenderScope scope)
        {
            switch (scope)
            {
                case GenderScope.Male: return p.gender == Gender.Male;
                case GenderScope.Female: return p.gender == Gender.Female;
                default: return true;
            }
        }

        private static bool InheritableMatches(HediffDef trait, InheritableFilter mode)
        {
            switch (mode)
            {
                case InheritableFilter.Inheritable: return AnimalTraitsAccess.IsInheritable(trait);
                case InheritableFilter.NonInheritable: return !AnimalTraitsAccess.IsInheritable(trait);
                default: return true;
            }
        }

        private static List<Pawn> SelectToKeep(List<Pawn> slaughter, TraitTarget tt)
        {
            var keep = new List<Pawn>();
            if (!InheritableMatches(tt.trait, tt.inheritMode))
                return keep;

            var bearers = slaughter
                .Where(p => AnimalTraitsAccess.HasTrait(p, tt.trait) && AgeMatches(p, tt.ageScope) && GenderMatches(p, tt.genderScope))
                .OrderBy(p => p.ageTracker.AgeBiologicalTicks)
                .ToList();

            bool biasPair = tt.inheritMode != InheritableFilter.NonInheritable && AnimalTraitsAccess.IsInheritable(tt.trait);
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

        /// <summary>Animals of this kind that match a breeding ("keep") trait target and are protected from slaughter.</summary>
        public int CountKeptByTraits(ThingDef def)
        {
            if (!kindSettings.TryGetValue(def, out var ks) || ks?.keepTraits == null || ks.keepTraits.Count == 0)
                return 0;
            int n = 0;
            foreach (var pa in map.mapPawns.SpawnedColonyAnimals)
            {
                if (pa.def != def) continue;
                foreach (var tt in ks.keepTraits)
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
        public int CountIndividuallyProtected(ThingDef def)
        {
            int n = 0;
            foreach (var pa in map.mapPawns.SpawnedColonyAnimals)
                if (pa.def == def && protectedPawnIDs.Contains(pa.thingIDNumber)) n++;
            return n;
        }
    }
}
