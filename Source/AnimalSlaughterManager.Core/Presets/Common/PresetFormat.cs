namespace ASM;

/// <summary>The preset format versions.</summary>
public static class PresetFormat
{
    /// <summary>Version 1 — the formats of the released mod (main): the unified carrier,
    /// read by PresetSerializer + Legacy.</summary>
    public const int MainVersion = 1;

    /// <summary>The current format: separate models per scope (AllPresetFile/KindPresetFile/
    /// ListPresetFile), the version stamped on save. Bump when a model changes and add the
    /// matching migration in PresetRepository's readers.</summary>
    public const int CurrentVersion = 2;
}
