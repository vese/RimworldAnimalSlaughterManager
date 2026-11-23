using System.Reflection;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>Pregnancy helpers for the slaughter engine: the Pregnant hediff of mammals plus developing eggs of egg-layers.</summary>
public static class PregnancyUtility
{
    private static readonly FieldInfo EggProgressField = typeof(CompEggLayer).GetField("eggProgress", BindingFlags.NonPublic | BindingFlags.Instance);

    // "Pregnant" for slaughter purposes: a mammal with the Pregnant hediff, or an egg-layer
    // carrying a developing egg (egg progress > 0).
    public static bool IsPregnantOrCarryingEgg(Pawn? p)
    {
        if (p == null)
        {
            return false;
        }
        var hs = p.health?.hediffSet;
        if (hs != null && hs.HasHediff(HediffDefOf.Pregnant))
        {
            return true;
        }
        return EggProgress(p.TryGetComp<CompEggLayer>()) > 0f;
    }

    // Pregnancy "severity" for ordering the pregnant bucket: the Pregnant hediff severity, or for
    // egg-layers the egg progress (0..1). Falls back to 0 (egg-layers have no Pregnant hediff).
    public static float PregnancyProgress(Pawn p)
    {
        var preg = p.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Pregnant);
        if (preg != null)
        {
            return preg.Severity;
        }
        return EggProgress(p.TryGetComp<CompEggLayer>());
    }

    // CompEggLayer.eggProgress is private — RimWorld exposes no public accessor for an
    // in-progress egg — so it is read via reflection. Returns 0 when unavailable.
    private static float EggProgress(CompEggLayer? egg)
    {
        if (egg == null || EggProgressField == null)
        {
            return 0f;
        }

        try
        {
            return (float)EggProgressField.GetValue(egg);
        }
        catch
        {
            return 0f;
        }
    }
}
