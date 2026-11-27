using System.Linq;

using Verse;

namespace ASM;

/// <summary>The version-2 conversion of the all-kinds Settings model (AllKindsPreset).</summary>
public class V2AllKindsPresetConverter : IV2PresetConverter<AllKindsPreset>
{
    public PresetFile ToFile(AllKindsPreset model, PresetScope scope, string? bucket = null) => new()
    {
        Version = PresetFormat.CurrentVersion,
        Scope = scope,
        Bucket = bucket,
        Type = nameof(V2AllKindsPreset),
        Content = PresetSerializer.SerializeContent(new V2AllKindsPreset { Kinds = [.. model.Kinds.Select(V2KindPresetConverter.ToContent)] })
    };

    public AllKindsPreset? ToModel(PresetFile file, ThingDef? targetDef = null) =>
        PresetSerializer.ReadContent<V2AllKindsPreset>(file) is { } content
            ? new AllKindsPreset { Kinds = [.. content.Kinds.Select(V2KindPresetConverter.ToModel)] }
            : null;
}
