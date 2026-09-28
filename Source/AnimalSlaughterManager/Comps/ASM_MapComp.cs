using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// <summary>
/// Per-map settings and state for Animal Slaughter Manager: per-kind settings, individual
/// protection, pregnant modes, global settings. The recomputed slaughter list itself is built
/// by <see cref="SlaughterListBuilder"/> and cached here, replacing the vanilla
/// <see cref="AutoSlaughterManager.AnimalsToSlaughter"/> result when any customization is active.
/// </summary>
public class ASM_MapComp : MapComponent
{
    public Dictionary<ThingDef, KindSettings> kindSettings = new Dictionary<ThingDef, KindSettings>();
    public HashSet<int> protectedPawnIDs = new HashSet<int>();
    public Dictionary<ThingDef, PregnantMode> pregnantModes = new Dictionary<ThingDef, PregnantMode>();

    // Global slaughter settings (General tab) — the counterpart of kindSettings.
    public GlobalSettings globalSettings = new();

    public bool dirty = true;
    public List<Pawn> cachedList = new List<Pawn>();

    public ASM_MapComp(Map map) : base(map)
    {
        SettingsChanges.Changed += MarkDirty;
    }

    public override void MapRemoved()
    {
        base.MapRemoved();
        SettingsChanges.Changed -= MarkDirty;
    }

    public bool AnyCustomization => protectedPawnIDs.Count > 0 || kindSettings.Values.Any(k => k.Customized) || pregnantModes.Count > 0;

    // True if this kind has per-kind pref overrides or condition lists (differs from the global defaults).
    public bool KindHasCustomPrefs(ThingDef def)
    {
        if (!kindSettings.TryGetValue(def, out var ks) || ks == null)
        {
            return false;
        }
        return !ks.preferenceSettings.Matches(globalSettings.preferenceSettings) || ks.prioritySettings.HasRules;
    }

    public KindSettings GetSettings(ThingDef def)
    {
        if (!kindSettings.TryGetValue(def, out var s))
        {
            s = new KindSettings(globalSettings);
            kindSettings[def] = s;
        }

        return s;
    }

    public bool IsProtected(Pawn p) => p != null && protectedPawnIDs.Contains(p.thingIDNumber);

    // Resolve the pregnant mode for a kind: an explicit per-kind choice, otherwise fall back to
    // the vanilla allowSlaughterPregnant flag (so existing saves keep their old behavior until
    // the user picks a mode via the 3-state toggle).
    public PregnantMode ResolvePregnantMode(ThingDef def, bool allowSlaughterPregnant)
        => pregnantModes.TryGetValue(def, out var m) ? m : (allowSlaughterPregnant ? PregnantMode.Always : PregnantMode.Never);

    public void ToggleProtected(Pawn p)
    {
        if (p == null)
        {
            return;
        }
        if (!protectedPawnIDs.Add(p.thingIDNumber))
        {
            protectedPawnIDs.Remove(p.thingIDNumber);
        }
        dirty = true;
    }

    public void MarkDirty() => dirty = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref kindSettings, "kindSettings", LookMode.Def, LookMode.Deep);
        Scribe_Collections.Look(ref protectedPawnIDs, "protectedPawnIDs", LookMode.Value);
        Scribe_Collections.Look(ref pregnantModes, "pregnantModes", LookMode.Def, LookMode.Value);
        globalSettings.ExposeData();
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (kindSettings == null)
            {
                kindSettings = new Dictionary<ThingDef, KindSettings>();
            }
            if (protectedPawnIDs == null)
            {
                protectedPawnIDs = new HashSet<int>();
            }
            if (pregnantModes == null)
            {
                pregnantModes = new Dictionary<ThingDef, PregnantMode>();
            }
        }
    }

    /// <summary>Rebuild the cached slaughter list (see <see cref="SlaughterListBuilder"/>).</summary>
    public void Recompute(AutoSlaughterManager mgr)
    {
        cachedList = SlaughterListBuilder.Build(this, mgr);
    }
}
