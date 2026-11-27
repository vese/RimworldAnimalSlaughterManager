using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>The condition buckets of the PriorityList scope. The folder names predate the
/// tuple keys — the same age×sex buckets as the settings themselves, shared with the
/// version-1 layout.</summary>
public class PriorityPresetBuckets : IPresetBucketProvider
{
    private static readonly ((bool Male, bool Adult)? Bucket, string Folder)[] buckets =
    [
        ((true, true), "AdultMale"),
        ((true, false), "YoungMale"),
        ((false, true), "AdultFemale"),
        ((false, false), "YoungFemale"),
    ];

    public PresetScope Scope => PresetScope.PriorityList;

    public IEnumerable<((bool Male, bool Adult)? Bucket, string Folder)> Buckets => buckets;

    /// <summary>The age×sex bucket's folder.</summary>
    public static string FolderOf((bool Male, bool Adult) bucket) => buckets.First(b => b.Bucket == bucket).Folder;
}
