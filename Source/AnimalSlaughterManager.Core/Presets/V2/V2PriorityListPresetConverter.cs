using System.Collections.Generic;

using Verse;

namespace ASM;

/// <summary>The version-2 conversion of a priority rule list (the Settings rule DTOs).</summary>
public class V2PriorityListPresetConverter : IV2PresetConverter<List<BasePriorityRulePreset>>
{
    public PresetFile ToFile(List<BasePriorityRulePreset> model, PresetScope scope, string? bucket = null) => new()
    {
        Version = PresetFormat.CurrentVersion,
        Scope = scope,
        Bucket = bucket,
        Type = nameof(PriorityListPresetFile),
        Content = PresetSerializer.SerializeContent(PriorityListPresetFile.OfRules(model))
    };

    public List<BasePriorityRulePreset>? ToModel(PresetFile file, PresetEntry entry, ThingDef? targetDef = null) =>
        PresetSerializer.ReadContent<PriorityListPresetFile>(file)?.Rules;
}
