using System;
using Verse;
namespace ASM;

/// <summary>The version-1 conversion of a special-settings list: main saves a List-scope file
/// as a single-kind carrier with one populated list — its sections (the raw text rides in the
/// synthesized envelope's content).</summary>
public class V1SpecialListPresetConverter : IV1PresetConverter<SpecialListPreset>
{
    public PresetFile ToFile(SpecialListPreset model, PresetScope scope, string? bucket = null) =>
        throw new NotSupportedException("The version-1 formats are read-only.");

    public SpecialListPreset? ToModel(PresetFile file, ThingDef? targetDef = null)
    {
#pragma warning disable CS0612, CS0618
        return LegacyPresets.TryReadSpecialList(file.Content);
#pragma warning restore CS0612, CS0618
    }
}
