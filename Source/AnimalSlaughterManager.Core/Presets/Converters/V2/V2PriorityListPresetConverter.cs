using System.Collections.Generic;

using Verse;

namespace ASM;

/// <summary>The version-2 conversion of a priority rule list (the Settings rule DTOs).</summary>
public class V2PriorityListPresetConverter : IV2PresetConverter<PriorityListPreset>
{
    public PresetFile ToFile(PriorityListPreset model, PresetScope scope, string? bucket = null) => new()
    {
        Version = PresetFormat.CurrentVersion,
        Scope = scope,
        Bucket = bucket,
        Type = nameof(V2PriorityListPreset),
        Content = PresetSerializer.SerializeContent(V2PriorityListPreset.OfRules(model.Rules))
    };

    public PriorityListPreset? ToModel(PresetFile file, ThingDef? targetDef = null) =>
        PresetSerializer.ReadContent<V2PriorityListPreset>(file)?.ToRuleList() is { } rules ? new PriorityListPreset { Rules = rules } : null;
}
