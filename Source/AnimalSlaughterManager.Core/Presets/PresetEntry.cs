using System;

namespace ASM;

public class PresetEntry
{
    public string name = null!;
    public string path = null!;
    public DateTime date;
    public PresetScope scope;
    public (bool Male, bool Adult)? condBucket;

    /// <summary>True if a management context of <paramref name="context"/> may delete this
    /// preset: only presets at least as narrow as the context itself.</summary>
    public bool DeletableFrom(PresetScope context) => scope >= context;
}
