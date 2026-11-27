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
            // Every bucket folder (cross-bucket import).
            foreach (var sub in Store.GetFolders(ScopeFolder(scope)))
            {
                AddFolder(sub, Path.GetFileName(sub));
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

    // ---- Save (the current-version serializer) -------------------------------------------

    /// <summary>Writes a preset of the current format (the content model in the file form) into
    /// its scope folder; a list preset goes into its bucket subfolder.</summary>
    public static void Save(string name, PresetScope scope, object content, string? bucket = null)
    {
        var folder = bucket == null ? ScopeFolder(scope) : Path.Combine(ScopeFolder(scope), bucket);
        Store.WriteText(Path.Combine(folder, name + ".xml"), PresetSerializer.SerializePreset(content, scope));
    }


    // ---- Read (the file's metadata, or the version-1 machinery by root) -------------------

    /// <summary>The full preset model; the version-1 unified format migrates into it.</summary>
    public static AllKindsPreset? ReadAll(PresetEntry entry)
    {
        var text = Store.ReadText(entry.path);
        var file = PresetSerializer.ReadPreset(text);

        if (file != null)
        {
            if (file.Scope is not PresetScope.All)
            {
                return null;
            }

            return PresetSerializer.ReadContent<AllKindsPresetFile>(file)?.ToPreset();
        }

#pragma warning disable CS0618
        return LegacyPresets.TryReadAll(text);
#pragma warning restore CS0618
    }

    /// <summary>The preset's kind slice: Kind-scope presets carry the single kind, All-scope
    /// presets the slice matching the target def. The caller applies it to the settings itself.</summary>
    public static KindPresetFile? ReadKind(PresetEntry entry, ThingDef targetDef)
    {
        var text = Store.ReadText(entry.path);
        var file = PresetSerializer.ReadPreset(text);

        if (file != null)
        {
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

#pragma warning disable CS0618
        return LegacyPresets.TryReadKind(entry, text, targetDef);
#pragma warning restore CS0618
    }

    /// <summary>A rule list of a List-scope preset, or the list slice of a Kind/All preset
    /// (cross-scope import); the version-1 formats migrate into the model. The payload is
    /// opaque — the owning settings class reads it. Null when nothing readable.</summary>
    public static ListPresetFile? ReadList(PresetEntry entry)
    {
        var text = Store.ReadText(entry.path);
        var file = PresetSerializer.ReadPreset(text);

        if (file != null)
        {
            // The metadata routes the content: a list preset carries it directly, a Kind/All
            // preset — its slice (cross-scope import).
            if (file.Scope is PresetScope.PriorityList or PresetScope.SpecialList)
            {
                return PresetSerializer.ReadContent<ListPresetFile>(file);
            }

            var slice = file.Scope == PresetScope.Kind
                ? PresetSerializer.ReadContent<KindPresetFile>(file)
                : PresetSerializer.ReadContent<AllKindsPresetFile>(file)?.Kinds.FirstOrDefault();

            return slice == null ? null : ListSliceOf(slice);
        }

#pragma warning disable CS0618
        return LegacyPresets.TryReadList(entry, text);
#pragma warning restore CS0618
    }

    // The list payload of a kind slice: the first non-empty rule bucket, or the raw
    // integration sections (a special-settings list); null when neither.
    private static ListPresetFile? ListSliceOf(KindPresetFile file)
    {
        var typed = new[] { file.Kind.Priorities.PrioRulesAdultMale, file.Kind.Priorities.PrioRulesYoungMale, file.Kind.Priorities.PrioRulesAdultFemale, file.Kind.Priorities.PrioRulesYoungFemale }
            .FirstOrDefault(rules => rules.Count > 0);

        return typed != null ? new ListPresetFile { Rules = typed }
            : file.Kind.SpecialSettings.Count > 0 ? ListPresetFile.OfSections(KindPresetFileSections.SectionsOf(file)) : null;
    }

    public static void Delete(PresetEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.path))
        {
            return;
        }
        Store.Delete(entry.path);
    }
}
