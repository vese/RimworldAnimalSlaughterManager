using System.Linq;

using Verse;

namespace ASM;

/// <summary>The version-2 conversion of a special-settings list (the Settings-side sections
/// wrapper; the sections are typed in the file form).</summary>
public class V2SpecialListPresetConverter : IV2PresetConverter<SpecialListPreset>
{
    public PresetFile ToFile(SpecialListPreset model, PresetScope scope, string? bucket = null) => new()
    {
        Version = PresetFormat.CurrentVersion,
        Scope = scope,
        Bucket = bucket,
        Type = nameof(V2SpecialListPreset),
        Content = PresetSerializer.SerializeContent(new V2SpecialListPreset
        {
            Sections = [.. model.Sections.Select(V2SpecialSettingsMapper.ToSection).Where(s => s != null)]
        })
    };

    public SpecialListPreset? ToModel(PresetFile file, PresetEntry entry, ThingDef? targetDef = null) =>
        PresetSerializer.ReadContent<V2SpecialListPreset>(file) is { } content
            ? new SpecialListPreset { Sections = [.. content.Sections.Select(V2SpecialSettingsMapper.ToElement)] }
            : null;
}
