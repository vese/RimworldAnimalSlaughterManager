using System.Linq;

using Verse;

namespace ASM;

/// <summary>The version-2 conversion of a special-settings list (the Settings-side typed
/// model).</summary>
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
            Settings = [.. model.Settings.Select(kv => V2Conversion.ToV2(kv.Value) is V2SpecialSettingsPreset v2 ? v2.WithKey(kv.Key) : null).Where(v2 => v2 != null)]
        })
    };

    public SpecialListPreset? ToModel(PresetFile file, ThingDef? targetDef = null)
    {
        if (PresetSerializer.DeserializeContent<V2SpecialListPreset>(file) is not { } content)
        {
            return null;
        }

        var model = new SpecialListPreset();

        foreach (var v2 in content.Settings)
        {
            if (V2Conversion.ToSettings(v2) is BaseSpecialSettingsPreset settings)
            {
                model.Settings[v2.Key] = settings;
            }
        }

        return model;
    }
}
