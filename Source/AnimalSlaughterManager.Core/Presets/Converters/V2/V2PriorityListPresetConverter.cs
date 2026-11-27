using System.Collections.Generic;
using System.Linq;

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
        Content = PresetSerializer.SerializeContent(new V2PriorityListPreset { Rules = [.. model.Rules.Select(r => V2Conversion.ToV2(r)).OfType<V2RulePreset>()] })
    };

    public PriorityListPreset? ToModel(PresetFile file, ThingDef? targetDef = null) =>
        PresetSerializer.DeserializeContent<V2PriorityListPreset>(file) is { } content
            ? new PriorityListPreset { Rules = [.. content.Rules.Select(r => V2Conversion.ToSettings(r)).OfType<BasePriorityRulePreset>()] }
            : null;
}
