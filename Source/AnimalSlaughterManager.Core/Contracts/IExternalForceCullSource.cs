using Verse;

namespace ASM;

/// <summary>Force-cull decided outside this mod's settings — by the foreign mod itself (its own
/// UI toggles). With no rules registered the whole pass is skipped.</summary>
public interface IExternalForceCullSource
{
    /// <summary>Whether any external cull decision exists at all (gates the pass).</summary>
    bool HasCullRules { get; }

    /// <summary>True when the foreign mod wants this animal culled.</summary>
    bool ShouldCull(Pawn pawn);
}
