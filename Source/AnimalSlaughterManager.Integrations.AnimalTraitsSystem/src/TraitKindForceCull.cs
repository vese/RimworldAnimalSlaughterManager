using Verse;

namespace ASM;

/// <summary>Force-cull by the kind's force-cull trait rules: matching animals are culled
/// regardless of count/limits (keep-reservation still wins).</summary>
public sealed class TraitKindForceCull : IKindForceCullSource
{
    public bool HasForceCullRules(KindSettings ks) => ks.specialRules.ForceCull.HasRules;

    public bool ShouldForceCull(Pawn pawn, KindSettings ks)
    {
        foreach (var rule in ks.specialRules.ForceCull.rules)
        {
            if (TraitRuleMatch.Matches(pawn, rule))
            {
                return true;
            }
        }

        return false;
    }
}
