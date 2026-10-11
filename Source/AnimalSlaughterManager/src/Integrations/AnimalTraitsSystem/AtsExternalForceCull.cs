using Verse;

namespace ASM;

/// <summary>Force-cull decided in the ATS mod's own UI (its cull-trait toggles).</summary>
public sealed class AtsExternalForceCull : IExternalForceCullSource
{
    public bool HasCullRules => AnimalTraitsAccess.GetCullTraitDefNames().Count > 0;

    public bool ShouldCull(Pawn pawn) =>
        AnimalTraitsAccess.ShouldCull(pawn, AnimalTraitsAccess.GetCullTraitDefNames());
}
