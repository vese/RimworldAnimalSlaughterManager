using System.Collections.Generic;

namespace ASM;

/// <summary>The per-kind slice of a preset: the preference and priority models and the
/// integration settings as their own objects (each integration owns its section format).
/// Built and read by KindSettings.ToPreset/FromPreset; the file form is Presets'
/// business.</summary>
public class KindPreset
{
    /// <summary>The kind's defName.</summary>
    public string AnimalDefName { get; set; } = null!;

    public PreferenceSettingsPreset Preferences { get; set; } = new();
    public PrioritySettingsPreset Priorities { get; set; } = new();

    /// <summary>The integration settings of the slice by their provider key; the file layer
    /// (de)serializes their sections.</summary>
    public Dictionary<string, IKindSettings> Integrations { get; set; } = [];
}
