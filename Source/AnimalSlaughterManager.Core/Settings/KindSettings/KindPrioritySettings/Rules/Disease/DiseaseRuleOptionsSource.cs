using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>Disease rule options of the add-rule menu: a specific disease (submenu) and any
/// disease at all.</summary>
public sealed class DiseaseRuleOptionsSource : IRuleOptionsSource
{
    public void AddRuleOptions(List<FloatMenuOption> options, bool male, bool adult, ThingDef animalDef, Action<BasePriorityRule> addRule)
    {
        // TODO: cache
        var diseaseList = DefDatabase<HediffDef>.AllDefs.Where(d => d.makesSickThought).OrderBy(d => d.LabelCap.ToString()).ToList();
        options.Add(new FloatMenuOption(ASMKeys.CondAddDisease.Translate(),
            () => OpenDiseaseSubmenu(diseaseList, addRule)));
        // TODO: 1 option
        AddOption(options, ASMKeys.CondDiseaseAnyHas, () => new DiseaseAnyPriorityRule { has = true }, addRule);
        AddOption(options, ASMKeys.CondDiseaseAnyMissing, () => new DiseaseAnyPriorityRule { has = false }, addRule);
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

    private static void AddOption(List<FloatMenuOption> options, TaggedString label, Func<BasePriorityRule> factory, Action<BasePriorityRule> addRule)
    {
        options.Add(new FloatMenuOption(label, () => addRule(factory())));
    }
}
