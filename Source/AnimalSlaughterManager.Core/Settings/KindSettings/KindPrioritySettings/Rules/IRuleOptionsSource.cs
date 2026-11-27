using System;
using System.Collections.Generic;
using Verse;

namespace ASM;

/// <summary>Contributes options to the add-rule menu of one priority bucket; every option
/// applies its rule through the addRule callback. The menu is assembled from all registered
/// sources — no single place knows the full rule list.</summary>
public interface IRuleOptionsSource
{
    void AddRuleOptions(List<FloatMenuOption> options, bool male, bool adult, ThingDef animalDef, Action<BasePriorityRule> addRule);
}
