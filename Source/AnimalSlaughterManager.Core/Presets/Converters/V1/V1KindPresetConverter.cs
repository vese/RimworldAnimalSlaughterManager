using System;
using Verse;

namespace ASM;

/// <summary>The version-1 conversion of one kind's Settings model: the old formats through
/// the Legacy machinery (the raw text rides in the synthesized envelope's content; the entry's
/// scope and the target def carry the slicing semantics).</summary>
public class V1KindPresetConverter : IV1PresetConverter<KindPreset>
{
    public PresetFile ToFile(KindPreset model, PresetScope scope, string? bucket = null) =>
        throw new NotSupportedException("The version-1 formats are read-only.");

    public KindPreset? ToModel(PresetFile file, PresetEntry entry, ThingDef? targetDef = null)
    {
#pragma warning disable CS0612, CS0618
        return LegacyPresets.TryReadKind(entry, file.Content, targetDef!);
#pragma warning restore CS0612, CS0618
    }
}
