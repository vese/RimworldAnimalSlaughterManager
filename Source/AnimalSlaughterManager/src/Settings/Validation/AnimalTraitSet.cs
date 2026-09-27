using System;
using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>Trait axis: per-trait «carries it / doesn't carry it» flags plus the aggregate
/// «any trait / positive / negative / foreign trait» axes. Trait combinations are not
/// enumerated — a specific-trait rule only depends on its own def.</summary>
public sealed class AnimalTraitSet : TraitSet<AnimalTraitSet>
{
    private enum StateVariant
    {
        Plain,
        ForeignPositive,
        ForeignNegative,
    }

    private static readonly StateVariant[] AllVariants = [StateVariant.Plain, StateVariant.ForeignPositive, StateVariant.ForeignNegative];

    private readonly Dictionary<string, bool> traits = new(); // defName → isBad (polarity)

    protected override void AcceptData(BasePriorityRule rule)
    {
        if (rule is TraitPriorityRule { trait: not null } t)
        {
            traits[t.trait.defName] = t.trait.isBad;
        }
    }

    public override void ValidateClosure(Action<int, string> addError)
    {
        var has = new Dictionary<string, int>();     // defName → index of the rule that closed «carries it»
        var notHas = new Dictionary<string, int>(); // … «doesn't carry it»
        int? anyTrait = null;                       // «carries at least one trait (incl. foreign)»
        int? noTrait = null;                        // «carries none of the mentioned traits»
        int? positive = null;                       // «carries a positive (or positive foreign) trait»
        int? noPositive = null;
        int? negative = null;
        int? noNegative = null;

        bool Covered(StateVariant variant, bool pos, bool neg, bool sEmpty) =>
            (anyTrait.HasValue && (!sEmpty || variant != StateVariant.Plain)) ||
            (noTrait.HasValue && sEmpty && variant == StateVariant.Plain) ||
            (positive.HasValue && pos) || (noPositive.HasValue && !pos) ||
            (negative.HasValue && neg) || (noNegative.HasValue && !neg);

        // An uncovered state exists in the box notHas.Keys ⊆ S ⊆ traits\has.Keys. Classes
        // (polarity profile × foreign variant × emptiness) are bounded by 15, not by 2^n.
        bool AnyUncovered(string? required, string? forbidden, Func<StateVariant, bool, bool, bool, bool>? inMatch)
        {
            var forced = notHas.Keys.ToHashSet();

            if (required != null)
            {
                forced.Add(required);
            }

            var banned = has.Keys.ToHashSet();

            if (forbidden != null)
            {
                banned.Add(forbidden);
            }

            if (forced.Overlaps(banned))
            {
                return false;
            }

            var optional = traits.Keys.Where(d => !forced.Contains(d) && !banned.Contains(d)).ToList();
            bool posForced = forced.Any(d => !traits[d]);
            bool negForced = forced.Any(d => traits[d]);
            bool posOptional = optional.Any(d => !traits[d]);
            bool negOptional = optional.Any(d => traits[d]);
            bool nonEmptyPossible = forced.Count > 0 || optional.Count > 0;

            foreach (var variant in AllVariants)
            {
                foreach (var sEmpty in new[] { true, false })
                {
                    if (sEmpty && forced.Count > 0)
                    {
                        continue;
                    }

                    if (!sEmpty && !nonEmptyPossible)
                    {
                        continue;
                    }

                    bool posT = posForced || (!sEmpty && posOptional);
                    bool posF = !posForced;
                    bool negT = negForced || (!sEmpty && negOptional);
                    bool negF = !negForced;

                    if (variant == StateVariant.Plain)
                    {
                        foreach (var pos in new[] { true, false })
                        {
                            foreach (var neg in new[] { true, false })
                            {
                                if ((pos && !posT) || (!pos && !posF) || (neg && !negT) || (!neg && !negF))
                                {
                                    continue;
                                }

                                if (inMatch == null || inMatch(variant, pos, neg, sEmpty))
                                {
                                    if (!Covered(variant, pos, neg, sEmpty))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        bool pos = variant == StateVariant.ForeignPositive;
                        bool neg = variant == StateVariant.ForeignNegative;

                        if (inMatch == null || inMatch(variant, pos, neg, sEmpty))
                        {
                            if (!Covered(variant, pos, neg, sEmpty))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        IEnumerable<int> AllClaimers()
        {
            var claimers = has.Values.Concat(notHas.Values).ToList();

            foreach (var claimer in new[] { anyTrait, noTrait, positive, noPositive, negative, noNegative })
            {
                if (claimer is int value)
                {
                    claimers.Add(value);
                }
            }

            return claimers;
        }

        foreach (var (index, rule) in OrderedRules())
        {
            switch (rule)
            {
                case TraitGeneralPriorityRule general:
                {
                    int? AxisClaimer() => (general.type, general.has) switch
                    {
                        (TraitType.Both, true) => anyTrait,
                        (TraitType.Both, false) => noTrait,
                        (TraitType.Positive, true) => positive,
                        (TraitType.Positive, false) => noPositive,
                        (TraitType.Negative, true) => negative,
                        (TraitType.Negative, false) => noNegative,
                        _ => null,
                    };

                    if (AxisClaimer() is int claimer)
                    {
                        MarkRedundant(addError, index, [claimer]);
                        continue;
                    }

                    Func<StateVariant, bool, bool, bool, bool> inMatch = (general.type, general.has) switch
                    {
                        (TraitType.Both, true) => (variant, _, _, sEmpty) => !sEmpty || variant != StateVariant.Plain,
                        (TraitType.Both, false) => (variant, _, _, sEmpty) => sEmpty && variant == StateVariant.Plain,
                        (TraitType.Positive, true) => (_, pos, _, _) => pos,
                        (TraitType.Positive, false) => (_, pos, _, _) => !pos,
                        (TraitType.Negative, true) => (_, _, neg, _) => neg,
                        (TraitType.Negative, false) => (_, _, neg, _) => !neg,
                        _ => (_, _, _, _) => false,
                    };

                    if (!AnyUncovered(null, null, inMatch))
                    {
                        MarkRedundant(addError, index, AllClaimers());
                        continue;
                    }

                    switch ((general.type, general.has))
                    {
                        case (TraitType.Both, true): anyTrait = index; break;
                        case (TraitType.Both, false): noTrait = index; break;
                        case (TraitType.Positive, true): positive = index; break;
                        case (TraitType.Positive, false): noPositive = index; break;
                        case (TraitType.Negative, true): negative = index; break;
                        case (TraitType.Negative, false): noNegative = index; break;
                    }

                    if (!AnyUncovered(null, null, null))
                    {
                        MarkExhausts(addError, index);
                    }

                    break;
                }

                case TraitPriorityRule { trait: not null } specific:
                {
                    var def = specific.trait.defName;
                    var flags = specific.has ? has : notHas;

                    if (flags.TryGetValue(def, out var flagCloser))
                    {
                        MarkRedundant(addError, index, [flagCloser]);
                        continue;
                    }

                    if (!AnyUncovered(specific.has ? def : null, specific.has ? null : def, null))
                    {
                        MarkRedundant(addError, index, AllClaimers());
                        continue;
                    }

                    flags[def] = index;

                    if (!AnyUncovered(null, null, null))
                    {
                        MarkExhausts(addError, index);
                    }

                    break;
                }
            }
        }
    }
}
