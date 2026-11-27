namespace ASM;

/// <summary>The preset format versions.</summary>
public static class PresetFormat
{
    /// <summary>The current format: one envelope for every preset (PresetEnvelope — the
    /// version, the scope and content-type metadata, the serialized content model). Files of
    /// the released mod (main) are the older root-element formats — the readers fall back to
    /// the v1 machinery by the root. Bump when the envelope or a model changes and add the
    /// matching migration.</summary>
    public const int CurrentVersion = 2;
}
