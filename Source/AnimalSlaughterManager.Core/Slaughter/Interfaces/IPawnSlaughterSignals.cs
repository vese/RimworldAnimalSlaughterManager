using Verse;

namespace ASM;

/// <summary>
/// Slaughter tie-breaker signals of one animal (cull earlier: fewer good signals, more bad
/// ones). The core sums every implementation's signals into the bucket sort — e.g. an
/// integration counts the good/bad traits it knows about.
/// </summary>
public interface IPawnSlaughterSignals
{
    (int Good, int Bad) SignalsOf(Pawn pawn);
}
