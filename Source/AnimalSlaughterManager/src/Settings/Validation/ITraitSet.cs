using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>
/// An accumulated trait set (pregnancy, training skills, traits, …) of one animal axis. The
/// validation context feeds rules into their sets; a set stores the rules with their list
/// indices and the concrete data it took from them, then runs the closure check over its axes.
/// </summary>
public interface ITraitSet
{
    /// <summary>Rules registered in this set by rule type, with their indices in the list.</summary>
    IReadOnlyDictionary<Type, List<(int index, BasePriorityRule rule)>> Rules { get; }

    /// <summary>Takes the rule with its index and accumulates the data it carries.</summary>
    void Accept(BasePriorityRule rule, int index);

    /// <summary>Closure check over the accumulated rules in list order: rules close axis
    /// states (per-def flags and aggregate axes); a rule whose states are all closed is
    /// redundant, a rule that closes the last open state exhausts the set.</summary>
    void ValidateClosure(Action<int, string> addProblem);
}
