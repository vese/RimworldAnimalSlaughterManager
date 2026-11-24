using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>Training rule options of the add-rule menu: a specific trainable (submenu,
/// availability depends on the kind) and the general training-level rules.</summary>
public sealed class TrainingRuleOptionsSource : IRuleOptionsSource
{
    public void AddRuleOptions(List<FloatMenuOption> options, bool male, bool adult, ThingDef animalDef, Action<BasePriorityRule> addRule)
    {
        options.Add(new FloatMenuOption(ASMKeys.CondAddTraining.Translate(),
            () => OpenTrainingSubmenu(animalDef, addRule)));
        AddOption(options, ASMKeys.CondTrainingNone, () => new TrainingGeneralPriorityRule { type = TrainingGeneralType.None }, addRule);
        AddOption(options, ASMKeys.CondTrainingPartial, () => new TrainingGeneralPriorityRule { type = TrainingGeneralType.Partial }, addRule);
        AddOption(options, ASMKeys.CondTrainingFull, () => new TrainingGeneralPriorityRule { type = TrainingGeneralType.Full }, addRule);
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

    private static void AddOption(List<FloatMenuOption> options, string labelKey, Func<BasePriorityRule> factory, Action<BasePriorityRule> addRule)
    {
        options.Add(new FloatMenuOption(labelKey.Translate(), () => addRule(factory())));
    }
}
