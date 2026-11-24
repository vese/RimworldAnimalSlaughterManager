namespace ASM;

/// <summary>Preset breadth: All-animals /// <summary>Preset breadth: All-animals &gt; single Kind &gt; single List.</summary>gt; single Kind /// <summary>Preset breadth: All-animals &gt; single Kind &gt; single List.</summary>gt; single List. The value order
/// mirrors the breadth (All is the broadest), so scope comparisons (`<=`, `>=`) gate what a
/// context may show or delete; the value name is also the preset folder name.</summary>
public enum PresetScope
{
    All,
    Kind,
    List
}
