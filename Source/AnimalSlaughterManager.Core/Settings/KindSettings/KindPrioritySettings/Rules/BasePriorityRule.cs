using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace ASM;

public abstract class BasePriorityRule : IExposable
{
    // DTO type -> the rule type's name, discovered once by probing every rule's ToPreset.
    private static readonly Dictionary<Type, string> ruleNames = [];

    /// <summary>Rules from their preset models: each preset fills its rule (found by the
    /// preset's rule — a parameterless-ctor rule of the matching DTO). Unknown presets are
    /// dropped.</summary>
    public static List<BasePriorityRule> FromPresets(IEnumerable<BasePriorityRulePreset> presets)
    {
        var rules = new List<BasePriorityRule>();

        foreach (var preset in presets)
        {
            if (RuleOf(preset.GetType()) is { } rule)
            {
                rule.FromPreset(preset);
                rules.Add(rule);
            }
        }

        return rules;
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

    /// <summary>
    /// True when this condition references a def (trait/disease/trainable) that failed
    /// to resolve on load (def-providing mod disabled). Such entries are no-ops and get pruned.
    /// </summary>
    public virtual bool HasNullDef { get; } = false;

    /// <summary>The rule's extra row dropdown: values, the current one and the setter. Null when
    /// the rule has none. A setter must raise SettingsChangeTracker after editing the rule (the global
    /// version invalidates every settings cache); the rule only supplies the data — rendering is
    /// the widget's business.</summary>
    public virtual IDropdownController? GetExtraDropdown() => null;

    /// <summary>The rule's DTO element name — the type discriminator in preset files.</summary>
    public abstract string DtoName { get; }

    /// <summary>The rule's own preset model (the concrete rule-preset type).</summary>
    public abstract BasePriorityRulePreset ToPreset();

    /// <summary>Reads the rule from its preset model.</summary>
    public abstract void FromPreset(BasePriorityRulePreset preset);

    public abstract string Label { get; }
    public abstract BasePriorityRule Clone();
    public abstract void ExposeData();

    /// <summary>The trait set types this rule belongs to; empty for a rule that takes no part in
    /// validation. The context asks the rule for them and does the rest itself.</summary>
    public virtual IEnumerable<Type> TraitSetTypes => Type.EmptyTypes;

    /// <summary>The validators this rule needs; they may read several trait sets.</summary>
    public virtual IEnumerable<IRuleSetValidator> Validators => [];

    public abstract bool Matches(Pawn? p);

    /// <summary>True when the other rule is an identical duplicate (same type, same target and
    /// parameters). Used by DuplicateValidator; coverage is ICoversRule<TOther>.</summary>
    public virtual bool IsDuplicate(BasePriorityRule other) => false;

    public void ChangeVariant()
    {
        ChangeVariantInternal();
        SettingsChangeTracker.Raise();
    }

    protected virtual void ChangeVariantInternal() { }
}
