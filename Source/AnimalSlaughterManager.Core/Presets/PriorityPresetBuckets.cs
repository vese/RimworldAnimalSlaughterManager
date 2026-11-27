using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>The condition buckets of the PriorityList scope. The folder names predate the
/// tuple keys — the same age×sex buckets as the settings themselves, shared with the
/// version-1 layout.</summary>
public static class PriorityPresetBuckets
{
    /// <summary>The age×sex buckets by their folder names.</summary>
    public static readonly Dictionary<string, (bool Male, bool Adult)> Buckets = new()
    {
        ["AdultMale"] = (true, true),
        ["YoungMale"] = (true, false),
        ["AdultFemale"] = (false, true),
        ["YoungFemale"] = (false, false),
    };

    private static readonly Dictionary<(bool Male, bool Adult), string> folders = Buckets.ToDictionary(kv => kv.Value, kv => kv.Key);

    /// <summary>The age×sex bucket's folder name.</summary>
    public static string GetBucket((bool Male, bool Adult) bucket) => folders[bucket];
}
