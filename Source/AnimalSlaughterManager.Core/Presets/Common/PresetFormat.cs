namespace ASM;

/// <summary>The preset format versions.</summary>
public static class PresetFormat
{
    /// <summary>The current format: one file form for every preset (PresetFile — the version,
    /// the scope and content-type metadata, the serialized content model). Files of the
    /// released mod (main) are the older root-element formats — the readers fall back to the
    /// v1 machinery by the root. Bump when the file form or a model changes and add the
    /// matching migration.</summary>
    public const int CurrentVersion = 2;
}
