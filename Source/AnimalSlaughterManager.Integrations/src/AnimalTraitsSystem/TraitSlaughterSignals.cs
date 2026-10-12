using Verse;

namespace ASM;

/// <summary>Tie-breaker signals of the bucket sort: good traits cull last, bad traits first.</summary>
public sealed class TraitSlaughterSignals : IPawnSlaughterSignals
{
    public (int Good, int Bad) SignalsOf(Pawn pawn)
    {
        int good = 0, bad = 0;

        foreach (var def in AnimalTraitsAccess.KnownTraitDefs)
        {
            if (AnimalTraitsAccess.HasTrait(pawn, def))
            {
                if (def.isBad)
                {
                    bad++;
                }
                else
                {
                    good++;
                }
            }
        }

        return (good, bad);
    }
}
