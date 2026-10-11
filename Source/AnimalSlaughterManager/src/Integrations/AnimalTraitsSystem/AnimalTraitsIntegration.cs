using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// The Animal Traits System integration: adapts <see cref="AnimalTraitsAccess"/> (the foreign
/// mod's API wrapper) to <see cref="ITraitsIntegration"/>. Registered at startup when ATS is
/// active; nothing outside this folder references ATS wrappers.
/// </summary>
public sealed class AnimalTraitsIntegration : ITraitsIntegration
{
    public const string PackageId = "luved.animaltraits";

    public bool Active => AnimalTraitsAccess.IsATSActive;

    public bool HasAvailableTraits => AnimalTraitsAccess.HasAvailableTraits;

    public IReadOnlyList<HediffDef> KnownTraitDefs => AnimalTraitsAccess.KnownTraitDefs;

    public bool HasTrait(Pawn? pawn, HediffDef? def) => AnimalTraitsAccess.HasTrait(pawn, def);

    public bool IsInheritable(HediffDef def) => AnimalTraitsAccess.IsInheritable(def);

    public IReadOnlyCollection<string> CullTraitDefNames => AnimalTraitsAccess.GetCullTraitDefNames();

    public bool ShouldCull(Pawn pawn, IReadOnlyCollection<string> cullTraitDefNames) =>
        AnimalTraitsAccess.ShouldCull(pawn, cullTraitDefNames as HashSet<string> ?? [.. cullTraitDefNames]);

    public Color TraitColor(HediffDef def) => AnimalTraitsAccess.TraitColor(def);

    public string TraitTip(HediffDef def) => AnimalTraitsAccess.TraitTip(def);

    public string FormatStatFactor(float factor) => AnimalTraitsAccess.FormatStatFactor(factor);

    public void AddRuleMenuOptions(List<FloatMenuOption> options, Action<BasePriorityRule> addRule)
    {
        // Trait options only when ATS trait content is actually loaded.
        if (!HasAvailableTraits)
        {
            return;
        }

        options.Add(new FloatMenuOption(ASMKeys.CondAddTrait.Translate(),
            () => Find.WindowStack.Add(new Dialog_TraitFlagPicker(picked =>
            {
                // TODO: 1 option for 1 trait def
                foreach ((HediffDef def, bool has) in picked)
                {
                    addRule(new TraitPriorityRule { has = has, trait = def });
                }
            }))));

        // TODO: 1 option with TraitType.Both
        AddOption(options, ASMKeys.CondPositiveHas, () => new TraitGeneralPriorityRule { has = true, type = TraitType.Positive }, addRule);
        AddOption(options, ASMKeys.CondPositiveMissing, () => new TraitGeneralPriorityRule { has = false, type = TraitType.Positive }, addRule);
        AddOption(options, ASMKeys.CondNegativeHas, () => new TraitGeneralPriorityRule { has = true, type = TraitType.Negative }, addRule);
        AddOption(options, ASMKeys.CondNegativeMissing, () => new TraitGeneralPriorityRule { has = false, type = TraitType.Negative }, addRule);
    }

    private static void AddOption(List<FloatMenuOption> options, string labelKey, Func<BasePriorityRule> factory, Action<BasePriorityRule> addRule) =>
        options.Add(new FloatMenuOption(labelKey.Translate(), () => addRule(factory())));

    public IKindSlaughterSettingsDialogTab? CreateSpecialRulesTab(KindSettings settings) =>
        new KindSlaughterSettingsDialogSpecialRulesTab(KindTraitsSettings.Of(settings));

    // ---- Slaughter-engine services ---------------------------------------------------------

    public bool HasKeepRules(KindSettings ks) => KindTraitsSettings.Of(ks).protectRuleSet.HasRules;

    public bool HasForceCullRules(KindSettings ks) => KindTraitsSettings.Of(ks).forceCullRuleSet.HasRules;

    public IReadOnlyCollection<Pawn> SelectKeepReserved(Map map, ThingDef kind, KindSettings ks)
    {
        var reserved = new List<Pawn>();
        var settings = KindTraitsSettings.Of(ks);

        if (!settings.protectRuleSet.HasRules)
        {
            return reserved;
        }

        var kindPawns = map.mapPawns.SpawnedColonyAnimals.Where(p => p.def == kind).ToList();

        foreach (var tt in settings.protectRuleSet.rules)
        {
            if (tt?.trait == null || tt.keepCount <= 0)
            {
                continue;
            }

            reserved.AddRange(SelectToKeep(kindPawns, tt));
        }

        return reserved;
    }

    public int CountKept(Map map, ThingDef kind, KindSettings ks)
    {
        var settings = KindTraitsSettings.Of(ks);
        int n = 0;

        foreach (var pa in map.mapPawns.SpawnedColonyAnimals)
        {
            if (pa.def != kind)
            {
                continue;
            }

            foreach (var tt in settings.protectRuleSet.rules)
            {
                if (tt.trait != null && HasTrait(pa, tt.trait)
                    && AgeMatches(pa, tt.ageScope) && GenderMatches(pa, tt.genderScope)
                    && InheritableMatches(tt.trait, tt.inheritMode))
                {
                    n++;
                    break;
                }
            }
        }

        return n;
    }

    public bool ShouldForceCullBySettings(Pawn pawn, KindSettings ks)
    {
        foreach (var ct in KindTraitsSettings.Of(ks).forceCullRuleSet.rules)
        {
            if (ct.trait != null && HasTrait(pawn, ct.trait)
                && AgeMatches(pawn, ct.ageScope) && GenderMatches(pawn, ct.genderScope)
                && InheritableMatches(ct.trait, ct.inheritMode))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AgeMatches(Pawn p, AgeScope scope)
    {
        bool repro = p.ageTracker.CurLifeStage.reproductive;

        switch (scope)
        {
            case AgeScope.Adult:
                return repro;
            case AgeScope.Young:
                return !repro;
            default:
                return true;
        }
    }

    private static bool GenderMatches(Pawn p, GenderScope scope)
    {
        switch (scope)
        {
            case GenderScope.Male:
                return p.gender == Gender.Male;
            case GenderScope.Female:
                return p.gender == Gender.Female;
            default:
                return true;
        }
    }

    private static bool InheritableMatches(HediffDef trait, TraitInheritability mode)
    {
        switch (mode)
        {
            case TraitInheritability.Inheritable:
                return AnimalTraitsAccess.IsInheritable(trait);
            case TraitInheritability.NonInheritable:
                return !AnimalTraitsAccess.IsInheritable(trait);
            default:
                return true;
        }
    }

    private static List<Pawn> SelectToKeep(List<Pawn> kindPawns, TraitProtectRule tt)
    {
        var keep = new List<Pawn>();

        if (!InheritableMatches(tt.trait, tt.inheritMode))
        {
            return keep;
        }

        var bearers = kindPawns
            .Where(p => AnimalTraitsAccess.HasTrait(p, tt.trait) && AgeMatches(p, tt.ageScope) && GenderMatches(p, tt.genderScope))
            .OrderBy(p => p.ageTracker.AgeBiologicalTicks)
            .ToList();

        bool biasPair = tt.inheritMode != TraitInheritability.NonInheritable && AnimalTraitsAccess.IsInheritable(tt.trait);

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
