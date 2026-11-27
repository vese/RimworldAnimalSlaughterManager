#pragma warning disable CS0612, CS0618

using System;
using System.Collections.Generic;
using System.IO;

namespace ASM;

/// <summary>
/// The version-1 list-preset layout: both list flavors shared the "List" root — the condition
/// buckets under <c>Conditions/</c>, the special lists in the other subfolders. Read only;
/// saving goes to the value-named scope folders. Delete together with the rest of the Legacy
/// folder when the disk-compatibility horizon allows — the caller is PresetRepository only.
/// </summary>
[Obsolete]
public static class LegacyListPresets
{
    private const string RootFolderName = "List";
    private const string FolderConditions = "Conditions";

    private static readonly IPresetStore Store = new FilePresetStore();

    /// <summary>Adds the v1 special-list presets: every subfolder of the shared root except
    /// the conditions tree.</summary>
    public static void AddSpecialListPresets(List<PresetEntry> result, string presetRoot, PresetScope scope)
    {
        foreach (var sub in Store.GetFolders(Path.Combine(presetRoot, RootFolderName)))
        {
            if (Path.GetFileName(sub) == FolderConditions)
            {
                continue;
            }

            AddFolder(result, sub, scope);
        }
    }

    /// <summary>Adds the v1 condition presets of every bucket folder
    /// (<c>List/Conditions/&lt;folder&gt;</c>), stamped with the bucket.</summary>
    public static void AddConditionPresets(List<PresetEntry> result, string presetRoot, PresetScope scope)
    {
        foreach (var (folder, bucket) in PriorityPresetBuckets.Buckets)
        {
            AddFolder(result, Path.Combine(presetRoot, RootFolderName, FolderConditions, folder), scope, bucket);
        }
    }

    private static void AddFolder(List<PresetEntry> result, string dir, PresetScope scope, (bool Male, bool Adult)? bucket = null)
    {
        foreach (var p in Store.GetFiles(dir))
        {
            result.Add(new PresetEntry { name = Path.GetFileNameWithoutExtension(p), path = p, date = Store.GetLastWriteTime(p), scope = scope, condBucket = bucket });
        }
    }
}
