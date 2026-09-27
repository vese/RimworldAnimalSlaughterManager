using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>
/// An accumulated trait set (pregnancy, training skills, traits, …) of one animal axis. The
/// validation context feeds rules into their sets; a set stores the rules with their list
/// indices and the concrete data it took from them, then distinguishes animal states and matches rules.
/// </summary>
public interface ITraitSet
{
    /// <summary>Rules registered in this set by rule type, with their indices in the list.</summary>
    IReadOnlyDictionary<Type, List<(int index, BasePriorityRule rule)>> Rules { get; }

    /// <summary>Takes the rule with its index and accumulates the data it carries.</summary>
    void Accept(BasePriorityRule rule, int index);

    /// <summary>Animal states this set distinguishes by its accumulated data.</summary>
    IReadOnlyList<object> EnumerateStates();

    /// <summary>Whether the rule matches the state; foreign-set rules match everything.</summary>
    bool Matches(BasePriorityRule rule, object state);
}
