using System.Reflection;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>Pawn properties the slaughter engine asks about: pregnancy incl. egg-layers.</summary>
public static class PawnSlaughterInfo
{
    private static readonly FieldInfo EggProgressField = typeof(CompEggLayer).GetField("eggProgress", BindingFlags.NonPublic | BindingFlags.Instance);

    // "Pregnant" for slaughter purposes: a mammal with the Pregnant hediff, or an egg-layer
    // carrying a developing egg (CompEggLayer.eggProgress > 0). eggProgress is private — RimWorld
    // exposes no public accessor for an in-progress egg — so it is read via reflection.
    public static bool IsPregnantOrCarryingEgg(Pawn? p)
    {
        if (p == null) return false;
        var hs = p.health?.hediffSet;
        if (hs != null && hs.HasHediff(HediffDefOf.Pregnant)) return true;
        var egg = p.TryGetComp<CompEggLayer>();
        if (egg != null && EggProgressField != null)
        {
            try { if ((float)EggProgressField.GetValue(egg) > 0f) return true; } catch { }
        }
        return false;
    }

    // Pregnancy "severity" for ordering the pregnant bucket: the Pregnant hediff severity, or for
    // egg-layers the egg progress (0..1). Falls back to 0 (also avoids NPE on egg-layers, which
    // have no Pregnant hediff).
    public static float PregnancyProgress(Pawn p)
    {
        var preg = p.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Pregnant);
        if (preg != null) return preg.Severity;
        var egg = p.TryGetComp<CompEggLayer>();
        if (egg != null && EggProgressField != null)
        {
            try { return (float)EggProgressField.GetValue(egg); } catch { }
        }
        return 0f;
    }
}
