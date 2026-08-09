using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// Access to Animal Traits System (ATS, package id <c>luved.animaltraits</c>) traits.
///
/// ATS stores each trait as a <see cref="HediffDef"/> whose defName starts with
/// "AnimalTrait_" ("AnimalTrait_Common" is the no-traits marker). Whether a trait is
/// inheritable is set on its <c>AnimalTraitExtension</c> mod extension
/// (<c>IsInheritable</c> = inheritChance &gt; 0). ATS already spares/culls animals based on
/// its own trait lists; we read traits and ATS's cull list via stable, public ATS surfaces
/// so our breeding targets <em>compose</em> with ATS rather than fight it.
///
/// All ATS types are reached by name (no compile-time dependency), so this compiles and
/// runs whether or not ATS is installed.
/// </summary>
public static class AnimalTraitsAccess
{
    public const string ATSPackageId = "luved.animaltraits";
    private const string TraitPrefix = "AnimalTrait_";
    private const string CommonMarker = "AnimalTrait_Common";
    private const string ReferenceTrait = "AnimalTrait_StatReference"; // ATS sample trait listing every stat — not a real trait.

    private static bool _atsResolved;
    public static Type? AtsGameComponentType
    {
        get
        {
            if (_atsResolved)
            {
                return field;
            }

            _atsResolved = true;

            foreach (var t in GenTypes.AllSubclasses(typeof(GameComponent)))
            {
                if (t.Name == "AnimalTraitsGameComponent")
                {
                    field = t;
                    break;
                }
            }

            return field;
        }
    }

    private static bool? _atsActive;
    public static bool IsATSActive
    {
        get
        {
            _atsActive ??= LoadedModManager.RunningMods.Any(m => m.PackageId == ATSPackageId);
            return _atsActive.Value;
        }
    }

    /// <summary>
    /// All ATS trait defs, sorted by label. Cached on first access — defs are immutable after load,
    /// so the scan runs exactly once. First access is always post-load (UI/gameplay paths), never
    /// during def resolution. The ATS dependency stub alone defines no traits, so a non-empty list
    /// is the precise "are traits usable" signal (more precise than <see cref="IsATSActive"/>).
    /// </summary>
    public static IReadOnlyList<HediffDef> KnownTraitDefs
    {
        get
        {
            field ??= [.. DefDatabase<HediffDef>.AllDefs.Where(IsAnimalTraitDef).OrderBy(d => d.label)];
            return field;
        }
    }

    public static bool IsAnimalTraitDef(HediffDef def) =>
        def != null && def.defName != null && def.defName.StartsWith(TraitPrefix) && def.defName != CommonMarker && def.defName != ReferenceTrait;

    /// <summary>
    /// True when at least one animal-trait def is actually defined (ATS trait content
    /// loaded). Reuses the <see cref="KnownTraitDefs"/> cache — a single DefDatabase scan total.
    /// </summary>
    public static bool HasAvailableTraits => KnownTraitDefs.Count > 0;

    public static bool HasTrait(Pawn p, HediffDef def)
    {
        if (p == null || def == null)
        {
            return false;
        }

        return p.health?.hediffSet?.HasHediff(def) ?? false;
    }

    private static readonly Color BadTraitColor = new Color(1f, 0.7f, 0.7f);
    private static readonly Color GoodTraitColor = new Color(0.7f, 1f, 0.7f);

    /// <summary>Green for beneficial traits, red for detrimental ones (vanilla HediffDef.isBad).</summary>
    public static Color TraitColor(HediffDef def) =>
        def == null ? Color.white : (def.isBad ? BadTraitColor : GoodTraitColor);

    /// <summary>Human-readable summary of what a trait does: description + stat/capacity modifiers.</summary>
    public static string TraitTip(HediffDef def)
    {
        if (def == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        if (!def.Description.NullOrEmpty())
        {
            sb.AppendLine(def.Description);
        }

        if (def.stages != null)
        {
            bool firstSection = true;

            foreach (var stage in def.stages)
            {
                if (stage == null)
                {
                    continue;
                }

                if (stage.statOffsets != null && stage.statOffsets.Count > 0)
                {
                    if (sb.Length > 0 && firstSection)
                    {
                        sb.AppendLine();
                    }

                    foreach (var sm in stage.statOffsets)
                    {
                        sb.AppendLine(sm.stat.LabelCap + ": " + sm.ValueToStringAsOffset);
                    }

                    firstSection = false;
                }

                if (stage.statFactors != null && stage.statFactors.Count > 0)
                {
                    if (sb.Length > 0 && firstSection)
                    {
                        sb.AppendLine();
                    }

                    foreach (var sm in stage.statFactors)
                    {
                        sb.AppendLine(sm.stat.LabelCap + ": " + FormatStatFactor(sm.value));
                    }

                    firstSection = false;
                }

                if (stage.capMods != null && stage.capMods.Count > 0)
                {
                    if (sb.Length > 0 && firstSection)
                    {
                        sb.AppendLine();
                    }

                    foreach (var cm in stage.capMods)
                    {
                        sb.AppendLine(cm.capacity.LabelCap + ": " + cm.offset.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Offset));
                    }

                    firstSection = false;
                }
            }
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Formats a stat factor multiplier (1.0 = no change) as a signed percent, e.g. 1.05 → "+5%", 0.9 → "-10%".</summary>
    public static string FormatStatFactor(float factor)
    {
        var pct = Mathf.Round((factor - 1f) * 100f);
        return (pct >= 0 ? "+" : "-") + Mathf.Abs(pct).ToString("0") + "%";
    }

    /// <summary>ATS's per-trait inheritable flag, read off the mod extension by name.</summary>
    public static bool IsInheritable(HediffDef def)
    {
        if (def == null)
        {
            return false;
        }

        var ext = def.modExtensions?.FirstOrDefault(e => e != null && e.GetType().Name == "AnimalTraitExtension");

        if (ext == null)
        {
            return false;
        }

        try
        {
            var prop = ext.GetType().GetProperty("IsInheritable");

            if (prop != null)
            {
                return (bool)prop.GetValue(ext, null);
            }

            var field = ext.GetType().GetField("inheritChance", BindingFlags.Public | BindingFlags.Instance);

            if (field != null)
            {
                return Convert.ToInt32(field.GetValue(ext)) > 0;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[ASM] IsInheritable failed for {def?.defName ?? "null"}: {ex}");
        }

        return false;
    }

    /// <summary>The trait defNames ATS is configured to force-cull, if ATS is present.</summary>
    public static HashSet<string> GetCullTraitDefNames()
    {
        var result = new HashSet<string>();
        
        if (!IsATSActive)
        {
            return result;
        }

        var t = AtsGameComponentType;
        
        if (t == null)
        {
            return result;
        }

        try
        {
            var current = t.GetProperty("Current")?.GetValue(null, null);

            if (current == null)
            {
                return result;
            }

            if (t.GetMethod("GetCullTraits")?.Invoke(current, null) is IEnumerable<string> list)
            {
                foreach (var s in list)
                {
                    if (s != null)
                    {
                        result.Add(s);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[ASM] GetCullTraitDefNames failed: {ex}");
        }

        return result;
    }

    public static bool ShouldCull(Pawn p, HashSet<string> cullDefNames)
    {
        if (p == null || cullDefNames == null || cullDefNames.Count == 0)
        {
            return false;
        }

        var hediffs = p.health?.hediffSet?.hediffs;

        if (hediffs == null)
        {
            return false;
        }

        foreach (var h in hediffs)
        {
            if (h?.def != null && cullDefNames.Contains(h.def.defName))
            {
                return true;
            }
        }

        return false;
    }
}
