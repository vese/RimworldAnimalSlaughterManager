using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>The condition buckets of the PriorityList scope. The folder names predate the
/// tuple keys — the same age×sex buckets as the settings themselves, shared with the
/// version-1 layout.</summary>
public static class PriorityPresetBuckets
{
    private static readonly Dictionary<(bool Male, bool Adult), string> folders = new()
    {
        [(true, true)] = "AdultMale",
        [(true, false)] = "YoungMale",
        [(false, true)] = "AdultFemale",
        [(false, false)] = "YoungFemale",
    };

    private static readonly Dictionary<string, (bool Male, bool Adult)> buckets = folders.ToDictionary(kv => kv.Value, kv => kv.Key);

    /// <summary>The age×sex bucket's folder name.</summary>
    public static string GetBucket((bool Male, bool Adult) bucket) => folders[bucket];

    /// <summary>The age×sex keys of a folder name; null when the folder is not a condition
    /// bucket.</summary>
    public static (bool Male, bool Adult)? GetKeys(string? folder) =>
        folder != null && buckets.TryGetValue(folder, out var keys) ? keys : null;
}
