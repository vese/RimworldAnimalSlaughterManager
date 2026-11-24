namespace ASM;

/// <summary>Preset breadth: All-animals &gt; single Kind &gt; single List. The value order
/// mirrors the breadth (All is the broadest), so scope comparisons (`<=`, `>=`) gate what a
/// context may show or delete; the value name is also the preset folder name.</summary>
public enum PresetScope
{
    All,
    Kind,
    List
}

/// <summary>The preset format versions.</summary>
public static class PresetFormat
{
    /// <summary>The preset format's version as saved today. Files written before versioning carry no
    /// attribute and read as 0 — the model shape at introduction. Bump this when the model
    /// changes and add the matching migration step in PresetSerializer.ReadPreset.</summary>
    public const int CurrentVersion = 1;
}
