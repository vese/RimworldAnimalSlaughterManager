using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// Breeding-stock reservation: animals of a kind the integration reserves from slaughter
/// (they still count toward the cull thresholds, so they take part of the limit).
/// </summary>
public interface IKeepReserveSource
{
    /// <summary>Animals of the kind to reserve as breeding stock.</summary>
    IReadOnlyCollection<Pawn> SelectKeepReserved(Map map, ThingDef kind, KindSettings ks);

    /// <summary>Animals of the kind currently matching a keep rule.</summary>
    int CountKept(Map map, ThingDef kind, KindSettings ks);

    /// <summary>The kind has keep rules at all.</summary>
    bool HasKeepRules(KindSettings ks);
}
