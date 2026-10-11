using Verse;

namespace ASM;

/// <summary>Force-cull rules of the integration's own per-kind settings: matching animals are
/// culled regardless of count/limits (keep-reservation still wins).</summary>
public interface IKindForceCullSource
{
    /// <summary>True when the animal matches a force-cull rule of the kind's settings.</summary>
    bool ShouldForceCull(Pawn pawn, KindSettings ks);

    /// <summary>The kind has force-cull rules at all.</summary>
    bool HasForceCullRules(KindSettings ks);
}
