namespace ASM;

/// <summary>The condition buckets of the PriorityList scope. The folder names predate the
/// tuple keys — the same age×sex buckets as the settings themselves, shared with the
/// version-1 layout.</summary>
public static class PriorityPresetBuckets
{
    /// <summary>The age×sex bucket's folder name.</summary>
    public static string GetBucket((bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => "AdultMale",
        (true, false) => "YoungMale",
        (false, true) => "AdultFemale",
        _ => "YoungFemale",
    };
}
