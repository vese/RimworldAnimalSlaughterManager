namespace ASM;

/// <summary>Preset breadth: All-animals &gt; single Kind &gt; a single rule list. The two list
/// scopes are disjoint flavors (priority conditions and special-settings lists) that never
/// apply to each other; both share the "List" preset folder. The value order mirrors the
/// breadth (All is the broadest), so scope comparisons (`<=`, `>=`) gate what a context may
/// show or delete.</summary>
public enum PresetScope
{
    All,
    Kind,
    PriorityList,
    SpecialList
}
