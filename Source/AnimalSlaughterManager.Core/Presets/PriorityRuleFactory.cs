using System;
using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>The rule factory: materializes the rule DTOs into runtime rules — the DTO type
/// maps to its rule by probing every rule type's ToPreset once.</summary>
public static class PriorityRuleFactory
{
    // DTO type -> (rule name, factory), discovered once by probing the rule types.
    private static readonly Dictionary<Type, string> ruleNames = [];

    /// <summary>Rules from the DTOs: each DTO fills its rule (found by the DTO's rule).
    /// Unknown DTOs are dropped.</summary>
    public static List<BasePriorityRule> ToRules(IEnumerable<BasePriorityRulePreset> presets)
    {
        var result = new List<BasePriorityRule>();

        foreach (var preset in presets)
        {
            var rule = RuleOf(preset.GetType());

            if (rule != null)
            {
                rule.FromPreset(preset);
                result.Add(rule);
            }
        }

        return result;
    }

    /// <summary>The rule's name for its DTO type (the v1 payload discriminator).</summary>
    internal static string RuleNameOf(Type dtoType)
    {
        EnsureMap();

        return ruleNames.TryGetValue(dtoType, out var name) ? name : "";
    }

    private static BasePriorityRule? RuleOf(Type dtoType)
    {
        EnsureMap();

        return ruleNames.TryGetValue(dtoType, out var name) ? RuleByName(name) : null;
    }

    private static BasePriorityRule? RuleByName(string ruleName)
    {
        var type = Type.GetType("ASM." + ruleName, false);

        if (type == null || !typeof(BasePriorityRule).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
        {
            return null;
        }

        return (BasePriorityRule)Activator.CreateInstance(type)!;
    }

    // Probes every rule type once: its ToPreset() reveals the DTO type.
    private static void EnsureMap()
    {
        if (ruleNames.Count > 0)
        {
            return;
        }

        foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
        {
            if (!typeof(BasePriorityRule).IsAssignableFrom(type) || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
            {
                continue;
            }

            var rule = (BasePriorityRule)Activator.CreateInstance(type)!;
            ruleNames[rule.ToPreset().GetType()] = type.Name;
        }
    }
}
