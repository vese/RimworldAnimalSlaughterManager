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

    /// <summary>Writes a preset DTO of the current format into its scope folder; a list preset
    /// goes into its bucket subfolder.</summary>
    public static void Save(string name, PresetScope scope, IVersionedPresetDto dto, string? bucket = null) =>
        Serialize(name, bucket == null ? ScopeFolder(scope) : Path.Combine(ScopeFolder(scope), bucket), dto);


    // ---- Read (dispatch by root: the current formats, or the version-1 machinery) ---------

    /// <summary>The full preset model; the version-1 unified format migrates into it.</summary>
    public static AllPreset? ReadAll(PresetEntry entry)
    {
        var text = Store.ReadText(entry.path);

        if (PresetSerializer.RootElementOf(text) == PresetSerializer.AllPresetRoot)
        {
            return PresetSerializer.DeserializePreset<AllPresetFile>(text)?.ToPreset();
        }

        var v1 = PresetSerializer.ReadV1Kinds(text);

        return v1 == null ? null : new AllPreset { Kinds = v1 };
    }

    /// <summary>The preset's kind slice: Kind-scope presets carry the single kind, All-scope
    /// presets the slice matching the target def. The caller applies it to the settings itself.</summary>
    public static KindPresetFile? ReadKind(PresetEntry entry, ThingDef targetDef)
    {
        var text = Store.ReadText(entry.path);

        if (PresetSerializer.RootElementOf(text) == PresetSerializer.KindPresetRoot)
        {
            return PresetSerializer.DeserializePreset<KindPresetFile>(text);
        }

        var kinds = PresetSerializer.ReadV1Kinds(text);

        if (kinds == null || kinds.Count == 0)
        {
            return null;
        }

        if (entry.scope == PresetScope.All)
        {
            // Find the slice for the target kind; if absent, nothing applies.
            return new KindPresetFile { Kind = kinds.FirstOrDefault(k => k.AnimalDefName == targetDef.defName) };
        }

        // Kind- and List-scope presets store a single kind whose defName is the SOURCE kind —
        // on apply we ignore it and use the list/settings for the TARGET kind.
        return new KindPresetFile { Kind = kinds[0] };
    }

    /// <summary>A rule list of a List-scope preset, or the list slice of a Kind/All preset
    /// (cross-scope import); the version-1 formats migrate into the model. The payload is
    /// opaque — the owning settings class reads it. Null when nothing readable.</summary>
    public static ListPresetFile? ReadList(PresetEntry entry)
    {
        var text = Store.ReadText(entry.path);

        switch (PresetSerializer.RootElementOf(text))
        {
            case PresetSerializer.ListPresetRoot:
                return PresetSerializer.DeserializePreset<ListPresetFile>(text);
            case PresetSerializer.KindPresetRoot:
                return FromSliceV1(text, PresetSerializer.KindPresetRoot);
            case PresetSerializer.AllPresetRoot:
                return FromSliceV1(text, PresetSerializer.AllPresetRoot);
            case PresetSerializer.PresetRoot:
                return FromSliceV1(text, PresetSerializer.PresetRoot);
            case PresetSerializer.RulePresetRoot:
                return FromRules(V1Rules(PresetSerializer.ReadRulePreset(text)));
            default:
#pragma warning disable CS0618
                return FromRules(LegacyPresets.TryReadConditions(entry));
#pragma warning restore CS0618
        }
    }

    // The list payload of a kind slice: the first non-empty rule bucket, or the raw
    // integration sections (a special-settings list); both the current and the v1 kind forms.
    private static ListPresetFile? FromSliceV1(string? text, string root)
    {
        if (root == PresetSerializer.KindPresetRoot)
        {
            var file = PresetSerializer.DeserializePreset<KindPresetFile>(text);

            if (file == null)
            {
                return null;
            }

            var typed = new[] { file.Kind.Priorities.PrioRulesAdultMale, file.Kind.Priorities.PrioRulesYoungMale, file.Kind.Priorities.PrioRulesAdultFemale, file.Kind.Priorities.PrioRulesYoungFemale }
                .FirstOrDefault(rules => rules.Count > 0);

            return typed != null ? new ListPresetFile { Rules = typed }
                : ListPresetFile.OfSections(KindPresetFileSections.SectionsOf(file));
        }

        var kd = PresetSerializer.ReadV1Kinds(text)?.FirstOrDefault();

        if (kd == null)
        {
            return null;
        }

        var kdTyped = new[] { kd.Priorities.PrioRulesAdultMale, kd.Priorities.PrioRulesYoungMale, kd.Priorities.PrioRulesAdultFemale, kd.Priorities.PrioRulesYoungFemale }
            .FirstOrDefault(rules => rules.Count > 0);

        return kdTyped != null ? new ListPresetFile { Rules = kdTyped }
            : kd.SpecialSettings.Count > 0 ? ListPresetFile.OfSections(KindPresetFileSections.SectionsOf(new KindPresetFile { Kind = kd })) : null;
    }

#pragma warning disable CS0618
    // The pre-unification RulePresetDto format still yields attribute-form rules.
    private static ListPresetFile? FromRules(List<BasePriorityRule>? rules) =>
        rules == null ? null : new ListPresetFile { Rules = rules.Select(r => r.ToPreset()).ToList() };

    private static List<BasePriorityRule>? V1Rules(List<PriorityRuleDto>? dtos) =>
        dtos?.Select(r => r.ToRule()).Where(r => r != null).ToList();
#pragma warning restore CS0618

    public static void Delete(PresetEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.path))
        {
            return;
        }
        Store.Delete(entry.path);
    }

    // ---- Helpers -------------------------------------------------------------------------

    private static void Serialize(string name, string folder, IVersionedPresetDto dto)
    {
        Store.WriteText(Path.Combine(folder, Sanitize(name) + ".xml"), PresetSerializer.SerializePreset(dto));
    }

    private static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name.Trim();
    }
}
