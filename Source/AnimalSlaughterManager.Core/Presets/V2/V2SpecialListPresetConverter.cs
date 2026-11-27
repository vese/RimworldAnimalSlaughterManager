using Verse;

namespace ASM;

/// <summary>The version-2 conversion of a special-settings list (the Settings-side sections
/// wrapper).</summary>
public class V2SpecialListPresetConverter : IV2PresetConverter<SpecialListPreset>
{
    public PresetFile ToFile(SpecialListPreset model, PresetScope scope, string? bucket = null) => new()
    {
        Version = PresetFormat.CurrentVersion,
        Scope = scope,
        Bucket = bucket,
        Type = nameof(SpecialListPresetFile),
        Content = PresetSerializer.SerializeContent(SpecialListPresetFile.OfSections(model.Sections))
    };

    public SpecialListPreset? ToModel(PresetFile file, PresetEntry entry, ThingDef? targetDef = null) =>
        PresetSerializer.ReadContent<SpecialListPresetFile>(file) is { } content
            ? new SpecialListPreset { Sections = content.Sections }
            : null;
}
