namespace ASM;

/// <summary>The preset format versions.</summary>
public static class PresetFormat
{
    /// <summary>The current format: separate models per scope (AllPresetFile/KindPresetFile/
    /// ListPresetFile), the version stamped on save. Files of the released mod (main) carry no
    /// Version attribute — the readers fall back to the v1 machinery by their root elements.
    /// Bump when a model changes and add the matching migration in PresetRepository's readers.</summary>
    public const int CurrentVersion = 2;
}
