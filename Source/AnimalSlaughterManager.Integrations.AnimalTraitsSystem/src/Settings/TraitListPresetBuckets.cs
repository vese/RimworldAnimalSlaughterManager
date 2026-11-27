using System.Collections.Generic;

namespace ASM;

/// <summary>The ATS list kinds as SpecialList buckets: the keep and force-cull trait lists.</summary>
public class TraitListPresetBuckets : IPresetBucketProvider
{
    public PresetScope Scope => PresetScope.SpecialList;

    public IEnumerable<((bool Male, bool Adult)? Bucket, string Folder)> Buckets =>
        [(null, ProtectRuleSet.PresetsFolderName), (null, ForceCullRuleSet.PresetsFolderName)];
}
