using System.Collections.Generic;

namespace ASM;

/// <summary>The bucket folders of a list preset scope — the destinations a list preset saves
/// into inside the scope's folder. The flavor's owner provides them: the core — the condition
/// buckets, an integration — its list kinds (registered unconditionally, so the buckets stay
/// listed with the integration off).</summary>
public interface IPresetBucketProvider
{
    /// <summary>The list scope the buckets belong to.</summary>
    PresetScope Scope { get; }

    /// <summary>The bucket folders with the condition bucket they stamp on the entries (null
    /// for special lists).</summary>
    IEnumerable<((bool Male, bool Adult)? Bucket, string Folder)> Buckets { get; }
}
