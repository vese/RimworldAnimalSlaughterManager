using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>The vanilla rule options of the add-rule menu (pregnancy, bond, diseases,
/// training), contributed through the same contract the integrations use.</summary>
public sealed class VanillaRuleOptions : IRuleOptionsSource
{
    public void AddRuleOptions(List<FloatMenuOption> options, bool male, bool adult, ThingDef animalDef, Action<BasePriorityRule> addRule)
    {
        if (!male && adult)
        {
            // TODO: 1 option
            AddOption(options, ASMKeys.CondPregnantHas, () => new PregnancyPriorityRule { has = true }, addRule);
            AddOption(options, ASMKeys.CondPregnantMissing, () => new PregnancyPriorityRule { has = false }, addRule);
        }

        // TODO: 1 option
        AddOption(options, ASMKeys.CondBondHas, () => new BondPriorityRule { has = true }, addRule);
        AddOption(options, ASMKeys.CondBondMissing, () => new BondPriorityRule { has = false }, addRule);

        // TODO: cache
        var diseaseList = DefDatabase<HediffDef>.AllDefs.Where(d => d.makesSickThought).OrderBy(d => d.LabelCap.ToString()).ToList();
        options.Add(new FloatMenuOption(ASMKeys.CondAddDisease.Translate(),
            () => OpenDiseaseSubmenu(diseaseList, addRule)));
        // TODO: 1 option
        AddOption(options, ASMKeys.CondDiseaseAnyHas, () => new DiseaseAnyPriorityRule { has = true }, addRule);
        AddOption(options, ASMKeys.CondDiseaseAnyMissing, () => new DiseaseAnyPriorityRule { has = false }, addRule);

        options.Add(new FloatMenuOption(ASMKeys.CondAddTraining.Translate(),
            () => OpenTrainingSubmenu(animalDef, addRule)));
        AddOption(options, ASMKeys.CondTrainingNone, () => new TrainingGeneralPriorityRule { type = TrainingGeneralType.None }, addRule);
        AddOption(options, ASMKeys.CondTrainingPartial, () => new TrainingGeneralPriorityRule { type = TrainingGeneralType.Partial }, addRule);
        AddOption(options, ASMKeys.CondTrainingFull, () => new TrainingGeneralPriorityRule { type = TrainingGeneralType.Full }, addRule);
    }

    private static void AddOption(List<FloatMenuOption> options, string labelKey, Func<BasePriorityRule> factory, Action<BasePriorityRule> addRule)
    {
        options.Add(new FloatMenuOption(labelKey.Translate(), () => addRule(factory())));
    }

    private static void AddOption(List<FloatMenuOption> options, TaggedString label, Func<BasePriorityRule> factory, Action<BasePriorityRule> addRule)
    {
        options.Add(new FloatMenuOption(label, () => addRule(factory())));
    }

    private static void OpenDiseaseSubmenu(List<HediffDef> defs, Action<BasePriorityRule> addRule)
    {
        // TODO: cache
        var dupLabels = defs
            .GroupBy(d => d.LabelCap.ToString())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        var options = new List<FloatMenuOption>();

        foreach (var def in defs)
        {
            var label = def.LabelCap.ToString();

            if (dupLabels.Contains(label))
            {
                label = $"{label} ({def.defName})";
            }

            AddOption(options, ASMKeys.CondHasSub.Translate(label), () => new DiseasePriorityRule { has = true, disease = def }, addRule);

            // TODO: 1 option
            AddOption(options, ASMKeys.CondMissingSub.Translate(label), () => new DiseasePriorityRule { has = false, disease = def }, addRule);
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    // Training submenu: available trainables for this kind first (white), unavailable below (gray).
    private static void OpenTrainingSubmenu(ThingDef animalDef, Action<BasePriorityRule> addRule)
    {
        var trainability = animalDef.race?.trainability;

        var options = new List<FloatMenuOption>();

        bool Avail(TrainableDef td) => trainability != null && td.requiredTrainability != null &&
            td.requiredTrainability.intelligenceOrder <= trainability.intelligenceOrder;

        // TODO: cache list
        foreach (var def in DefDatabase<TrainableDef>.AllDefs.OrderBy(t => t.LabelCap.ToString()).OrderByDescending(Avail))
        {
            var avail = Avail(def);
            var icon = string.IsNullOrEmpty(def.icon) ? null : def.Icon;
            var col = avail ? Color.white : Color.gray;

            void Add(string key, bool has) =>
                options.Add(avail ?
                    new FloatMenuOption(key.Translate(def.LabelCap),
                        () => addRule(new TrainingPriorityRule() { has = has, trainable = def }), icon, col) :
                    new GrayFloatMenuOption(key.Translate(def.LabelCap),
                        () => addRule(new TrainingPriorityRule() { has = has, trainable = def }), icon, col, col));

            Add(ASMKeys.CondTrainingLearnedSub, true);
            // TODO: 1 option
            Add(ASMKeys.CondTrainingNotSub, false);
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }
}
