using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace ASM;

/// <summary>
/// Export/import slaughter configuration so it can be reused across games.
///
/// Presets come in scopes, stored in subfolders of
/// <c>&lt;persistentDataPath&gt;/AnimalSlaughterManagerPresets/</c> named after the scope:
/// <list type="bullet">
/// <item><description><c>All/</c> — every configured kind (full settings). Also: legacy root
/// <c>*.xml</c> files are treated as All-scope for backward compatibility.</description></item>
/// <item><description><c>Kind/</c> — one kind's full settings (save for one kind, load into
/// another).</description></item>
/// <item><description><c>PriorityList/&lt;bucket&gt;/…</c> and <c>SpecialList/&lt;kind&gt;/…</c> —
/// a single rule list (save a list, load the corresponding list into another kind). The
/// version-1 shared <c>List/…</c> tree is still read.</description></item>
/// </list>
/// A higher-scope preset can be applied to a narrower target (All→kind, All→list, kind→list):
/// only the target's slice is taken. Higher-scope presets cannot be deleted from a narrower
/// context (see <see cref="PresetEntry.DeletableFrom"/>).
///
/// All file access goes through <see cref="Store"/> and paths are rooted at
/// <see cref="RootFolder"/> — both swappable, so the format logic is unit-testable.
/// </summary>
public static class PresetRepository
{
    // Preset tree layout under the game persistentDataPath.
    private const string RootFolderName = "AnimalSlaughterManagerPresets";

    /// <summary>File access of the preset subsystem; tests may substitute their own store.</summary>
    private static readonly IPresetStore Store = new FilePresetStore();

    private static string RootFolder => Path.Combine(Application.persistentDataPath, RootFolderName);

    // The scope folder is the enum value's own name.
    private static string ScopeFolder(PresetScope scope) => Path.Combine(RootFolder, scope.ToString());

    /// <summary>Presets of one scope (the list scopes — every bucket subfolder, each entry
    /// stamped with the folder name; both also read their version-1 folders), newest-first by
    /// file write time.</summary>
    public static List<PresetEntry> ListPresets(PresetScope scope)
    {
        var result = new List<PresetEntry>();

        void AddFolder(string dir, string? bucket = null)
        {
            foreach (var p in Store.GetFiles(dir))
            {
                result.Add(new PresetEntry
                {
                    name = Path.GetFileNameWithoutExtension(p),
                    path = p,
                    date = Store.GetLastWriteTime(p),
                    scope = scope,
                    bucket = bucket
                });
            }
        }

        if (scope is PresetScope.PriorityList or PresetScope.SpecialList)
        {
            // The folder is flat — the bucket rides in each file's metadata (cross-bucket
            // import included).
            foreach (var p in Store.GetFiles(ScopeFolder(scope)))
            {
                result.Add(new PresetEntry
                {
                    name = Path.GetFileNameWithoutExtension(p),
                    path = p,
                    date = Store.GetLastWriteTime(p),
                    scope = scope,
                    bucket = BucketOf(p)
                });
            }

            // The version-1 shared List root.
#pragma warning disable CS0618
            if (scope is PresetScope.SpecialList)
            {
                LegacyListPresets.AddSpecialListPresets(result, RootFolder, scope);
            }
            else
            {
                LegacyListPresets.AddConditionPresets(result, RootFolder, scope);
            }
#pragma warning restore CS0618
        }
        else
        {
            AddFolder(ScopeFolder(scope));
        }

        if (scope is PresetScope.All)
        {
            // Presets saved before scopes existed live in the root folder.
#pragma warning disable CS0618
            LegacyPresets.AddRootPresets(result, RootFolder);
#pragma warning restore CS0618
        }

        return [.. result.OrderByDescending(e => e.date).ThenBy(e => e.name)];
    }

    /// <summary>Writes a preset of the current format into its scope folder; the list bucket
    /// rides in the file's metadata, not the path.</summary>
    public static void Save(string name, PresetScope scope, object content, string? bucket = null)
    {
        Store.WriteText(Path.Combine(ScopeFolder(scope), name + ".xml"), PresetSerializer.SerializePreset(content, scope, bucket));
    }

    public static void Delete(PresetEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.path))
        {
            return;
        }

        Store.Delete(entry.path);
    }

    /// <summary>The full preset model; the version-1 unified format migrates into it.</summary>
    public static AllKindsPreset? ReadAllKindsSettings(PresetEntry entry)
    {
        var text = Store.ReadText(entry.path);
        var file = PresetSerializer.ReadPreset(text);

        if (file is null)
        {
#pragma warning disable CS0612
            return LegacyPresets.TryReadAll(text);
#pragma warning restore CS0612
        }

        if (file.Scope is not PresetScope.All)
        {
            return null;
        }

        return PresetSerializer.ReadContent<AllKindsPresetFile>(file)?.ToPreset();
    }

    /// <summary>The preset's kind slice: Kind-scope presets carry the single kind, All-scope
    /// presets the slice matching the target def. The caller applies it to the settings itself.</summary>
    public static KindPresetFile? ReadKindSettings(PresetEntry entry, ThingDef targetDef)
    {
        var text = Store.ReadText(entry.path);
        var file = PresetSerializer.ReadPreset(text);

        if (file is null)
        {
#pragma warning disable CS0612
            return LegacyPresets.TryReadKind(entry, text, targetDef);
#pragma warning restore CS0612
        }

        if (file.Scope is PresetScope.Kind)
        {
            return PresetSerializer.ReadContent<KindPresetFile>(file);
        }
        else if (file.Scope is PresetScope.All)
        {
            var match = PresetSerializer.ReadContent<AllKindsPresetFile>(file)?.Kinds
                .FirstOrDefault(k => k.Kind.AnimalDefName == targetDef.defName);

            return match == null ? null : new KindPresetFile { Kind = match.Kind };
        }

        return null;
    }

    /// <summary>A priority rule list of a PriorityList preset, or the matching bucket's slice
    /// of a Kind/All preset (cross-scope import); the version-1 formats migrate into the
    /// model. Null when nothing readable.</summary>
    public static PriorityListPresetFile? ReadPriorityList(PresetEntry entry, ThingDef targetDef, (bool Male, bool Adult) bucket)
    {
        var text = Store.ReadText(entry.path);
        var file = PresetSerializer.ReadPreset(text);

        if (file is null)
        {
#pragma warning disable CS0612
            return LegacyPresets.TryReadPriorityList(entry, text, targetDef, bucket);
#pragma warning restore CS0612
        }

        if (file.Scope is PresetScope.PriorityList)
        {
            return PresetSerializer.ReadContent<PriorityListPresetFile>(file);
        }
        else if (file.Scope is PresetScope.All or PresetScope.Kind)
        {
            var slice = GetKindSettings(file, targetDef);

            if (slice is null)
            {
                return null;
            }

            var rules = bucket switch
            {
                (true, true) => slice.Kind.Priorities.PriorityRulesAdultMale,
                (true, false) => slice.Kind.Priorities.PriorityRulesYoungMale,
                (false, true) => slice.Kind.Priorities.PriorityRulesAdultFemale,
                (false, false) => slice.Kind.Priorities.PriorityRulesYoungFemale
            };

            return rules.Count == 0 ? null : new PriorityListPresetFile { Rules = rules };
        }

        return null;
    }

    /// <summary>A special-settings list of a SpecialList preset, or the sections slice of a
    /// Kind/All preset (cross-scope import); the version-1 format migrates into the model.
    /// Null when nothing readable.</summary>
    public static SpecialListPresetFile? ReadSpecialList(PresetEntry entry, ThingDef targetDef)
    {
        var text = Store.ReadText(entry.path);
        var file = PresetSerializer.ReadPreset(text);

        if (file is null)
        {
#pragma warning disable CS0612
            return LegacyPresets.TryReadSpecialList(text, targetDef);
#pragma warning restore CS0612
        }

        if (file.Scope == PresetScope.SpecialList)
        {
            return PresetSerializer.ReadContent<SpecialListPresetFile>(file);
        }
        else if (file.Scope is PresetScope.All or PresetScope.Kind)
        {
            var slice = GetKindSettings(file, targetDef);

            if (slice is null || slice.Kind.SpecialSettings.Count == 0)
            {
                return null;
            }

            return SpecialListPresetFile.OfSections(KindPresetFileSections.SectionsOf(slice));
        }

        return null;
    }

    // Listings run every UI frame — the bucket comes from the file's metadata, parsed once
    // per file version (the write time).
    private static readonly Dictionary<string, KeyValuePair<DateTime, string?>> bucketCache = [];

    private static string? BucketOf(string path)
    {
        var time = Store.GetLastWriteTime(path);

        if (bucketCache.TryGetValue(path, out var cached) && cached.Key == time)
        {
            return cached.Value;
        }

        var bucket = PresetSerializer.ReadPreset(Store.ReadText(path))?.Bucket;
        bucketCache[path] = new(time, bucket);

        return bucket;
    }

    private static KindPresetFile? GetKindSettings(PresetFile file, ThingDef targetDef) =>
        file.Scope == PresetScope.Kind
            ? PresetSerializer.ReadContent<KindPresetFile>(file)
            : PresetSerializer.ReadContent<AllKindsPresetFile>(file)?.Kinds
                .FirstOrDefault(k => k.Kind.AnimalDefName == targetDef.defName);
}
