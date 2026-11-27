using System;
using System.Linq;
using Verse;
namespace ASM;

/// <summary>The version-1 conversion of a priority rule list: the RulePresetDto format or the
/// condition-preset format (the raw text rides in the synthesized envelope's content; the
/// cross-scope carrier slices are the repository's Kind/All path, not this converter).</summary>
public class V1PriorityListPresetConverter : IV1PresetConverter<PriorityListPreset>
{
    public PresetFile ToFile(PriorityListPreset model, PresetScope scope, string? bucket = null) =>
        throw new NotSupportedException("The version-1 formats are read-only.");

    public PriorityListPreset? ToModel(PresetFile file, ThingDef? targetDef = null)
    {
#pragma warning disable CS0612, CS0618
        var rules = LegacyPresets.RootElementOf(file.Content) == LegacyPresets.RuleRoot
            ? LegacyPresets.V1Rules(LegacyPresets.ReadRulePreset(file.Content))
            : LegacyPresets.TryReadConditions(file.Content);

        return rules is { Count: > 0 } ? new PriorityListPreset { Rules = [.. rules.Select(r => r.ToPreset())] } : null;
#pragma warning restore CS0612, CS0618
    }
}
