using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

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
            // Every bucket folder (cross-bucket import) — the bucket comes from the path, and
            // rides in each file's metadata as well.
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

    /// <summary>Writes a Settings preset model as a preset of the current format into its
    /// scope folder; a list preset goes into its bucket subfolder, and the bucket rides in the
    /// file's metadata too.</summary>
    public static void Save<TModel>(string name, PresetScope scope, TModel model, string? bucket = null)
        where TModel : class
    {
        var file = PresetConverterFactory.GetConverter<TModel>(new PresetFile { Version = PresetFormat.CurrentVersion })?.ToFile(model, scope, bucket)
            ?? throw new InvalidOperationException($"No converter registered for {typeof(TModel).Name}.");

        var folder = bucket == null ? ScopeFolder(scope) : Path.Combine(ScopeFolder(scope), bucket);
        Store.WriteText(Path.Combine(folder, name + ".xml"), PresetSerializer.Serialize(file));
    }

    public static void Delete(PresetEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.path))
        {
            return;
        }

        Store.Delete(entry.path);
    }

    /// <summary>The full preset model; the version-1 formats migrate into it.</summary>
    public static AllKindsPreset? ReadAllKindsSettings(PresetEntry entry)
    {
        var file = GetFile(entry);

        return PresetConverterFactory.GetConverter<AllKindsPreset>(file)?.ToModel(file, entry);
    }

    /// <summary>The preset's kind slice: Kind-scope presets carry the single kind, All-scope
    /// presets the slice matching the target def. The caller applies it to the settings itself.</summary>
    public static KindPreset? ReadKindSettings(PresetEntry entry, ThingDef targetDef)
    {
        var file = GetFile(entry);

        if (file.Version != 1 && file.Scope is PresetScope.All)
        {
            var match = PresetSerializer.ReadContent<V2AllKindsPreset>(file)?.Kinds
                .FirstOrDefault(k => k.AnimalDefName == targetDef.defName);

            return match == null ? null : V2KindPresetConverter.ToModel(match);
        }

        return PresetConverterFactory.GetConverter<KindPreset>(file)?.ToModel(file, entry, targetDef);
    }

    /// <summary>A priority rule list of a PriorityList preset, or the matching bucket's slice
    /// of a Kind/All preset (cross-scope import); the version-1 formats migrate into the
    /// model. Null when nothing readable.</summary>
    public static List<BasePriorityRulePreset>? ReadPriorityList(PresetEntry entry, ThingDef targetDef, (bool Male, bool Adult) bucket)
    {
        var file = GetFile(entry);

        if (file.Version == 1)
        {
#pragma warning disable CS0612, CS0618
            return LegacyPresets.TryReadPriorityList(entry, file.Content, targetDef, bucket)?.Rules;
#pragma warning restore CS0612, CS0618
        }

        if (file.Scope is PresetScope.PriorityList)
        {
            return PresetSerializer.ReadContent<PriorityListPresetFile>(file)?.Rules;
        }

        if (file.Scope is PresetScope.All or PresetScope.Kind)
        {
            var slice = GetKindSlice(file, targetDef);

            return slice == null ? null : GetRulesBucket(slice, bucket);
        }

        return null;
    }

    /// <summary>A special-settings list of a SpecialList preset, or the sections slice of a
    /// Kind/All preset (cross-scope import); the version-1 format migrates into the model.
    /// Null when nothing readable.</summary>
    public static SpecialListPreset? ReadSpecialList(PresetEntry entry, ThingDef targetDef)
    {
        var file = GetFile(entry);

        if (file.Version == 1)
        {
#pragma warning disable CS0612, CS0618
            return LegacyPresets.TryReadSpecialList(file.Content, targetDef) is { } legacy
                ? new SpecialListPreset { Sections = legacy.Sections }
                : null;
#pragma warning restore CS0612, CS0618
        }

        if (file.Scope == PresetScope.SpecialList)
        {
            return PresetSerializer.ReadContent<SpecialListPresetFile>(file) is { } content
                ? new SpecialListPreset { Sections = content.Sections }
                : null;
        }

        if (file.Scope is PresetScope.All or PresetScope.Kind)
        {
            var slice = GetKindSlice(file, targetDef);

            return slice?.Elements is { Length: > 0 } elements
                ? new SpecialListPreset { Sections = [.. elements.Where(e => !e.LocalName.StartsWith("PriorityRules"))] }
                : null;
        }

        return null;
    }

    // ---- Helpers -------------------------------------------------------------------------

    /// <summary>The entry's file form: the current format's envelope, or a synthesized one for
    /// a version-1 file (Version 1, the raw text as the content).</summary>
    private static PresetFile GetFile(PresetEntry entry)
    {
        var text = Store.ReadText(entry.path);

        return PresetSerializer.ReadPreset(text) ?? new PresetFile { Version = 1, Content = text };
    }

    // The kind slice a list import takes: a Kind preset's own kind (its defName is the SOURCE
    // kind — the import applies to the TARGET kind), an All preset's slice of the target kind.
    private static V2KindPreset? GetKindSlice(PresetFile file, ThingDef targetDef) =>
        file.Scope == PresetScope.Kind
            ? PresetSerializer.ReadContent<V2KindPreset>(file)
            : PresetSerializer.ReadContent<V2AllKindsPreset>(file)?.Kinds
                .FirstOrDefault(k => k.AnimalDefName == targetDef.defName);

    private static List<BasePriorityRulePreset>? GetRulesBucket(V2KindPreset slice, (bool Male, bool Adult) bucket)
    {
        var rules = V2KindPresetConverter.GetRules(slice, bucket);

        return rules.Count == 0 ? null : rules;
    }
}
