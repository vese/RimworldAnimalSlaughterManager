using System;
using Verse;

namespace ASM;

/// <summary>The version-1 conversion of the all-kinds Settings model: the old formats through
/// the Legacy machinery (the raw text rides in the synthesized envelope's content).</summary>
public class V1AllKindsPresetConverter : IV1PresetConverter<AllKindsPreset>
{
    public PresetFile ToFile(AllKindsPreset model, PresetScope scope, string? bucket = null) =>
        throw new NotSupportedException("The version-1 formats are read-only.");

    public AllKindsPreset? ToModel(PresetFile file, PresetEntry entry, ThingDef? targetDef = null)
    {
#pragma warning disable CS0612, CS0618
        return LegacyPresets.TryReadAll(file.Content);
#pragma warning restore CS0612, CS0618
    }
}
