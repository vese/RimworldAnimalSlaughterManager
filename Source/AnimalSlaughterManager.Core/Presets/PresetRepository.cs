using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// Export/import slaughter configuration so it can be reused across games.
///
/// Presets come in three scopes, stored in separate subfolders of
/// <c>&lt;persistentDataPath&gt;/AnimalSlaughterManagerPresets/</c>:
/// <list type="bullet">
/// <item><description><c>All/</c> — every configured kind (full settings). Also: legacy root
/// <c>*.xml</c> files are treated as All-scope for backward compatibility.</description></item>
/// <item><description><c>Kind/</c> — one kind's full settings (save for one kind, load into
/// another).</description></item>
/// <item><description><c>List/&lt;Keep|ForceCull|Conditions&gt;/…</c> — a single rule list for
/// one kind (save a list, load the corresponding list into another kind).</description></item>
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
    private const string FolderConditions = "Conditions";

    /// <summary>File access of the preset subsystem; tests may substitute their own store.</summary>
    private static readonly IPresetStore Store = new FilePresetStore();

    private static string RootFolder => Path.Combine(Application.persistentDataPath, RootFolderName);

    // The scope folder is the enum value's own name (All/Kind/List).
    private static string ScopeFolder(PresetScope scope) => Path.Combine(RootFolder, scope.ToString());

    /// <summary>Presets of one scope (List covers every rule-set subfolder), newest-first by file write time.</summary>
    public static List<PresetEntry> ListPresets(PresetScope scope)
    {
        var result = new List<PresetEntry>();

        void AddFolder(string dir)
        {
            foreach (var p in Store.GetFiles(dir))
            {
                result.Add(new PresetEntry
                {
                    name = Path.GetFileNameWithoutExtension(p),
                    path = p,
                    date = Store.GetLastWriteTime(p),
                    scope = scope
                });
            }
        }
        
        if (scope is PresetScope.List)
        {
            // Every rule-list kind writes into its own PresetsFolder subfolder — list them as
            // they are on disk, except the core's own conditions tree (it has its own browser).
            foreach (var sub in Store.GetFolders(ScopeFolder(scope)))
            {
                if (Path.GetFileName(sub) != FolderConditions)
                {
                    AddFolder(sub);
                }
            }
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

    /// <summary>Writes a preset DTO of the current format into its scope folder.</summary>
    public static void Save(string name, PresetScope scope, IVersionedPresetDto dto) =>
        Serialize(name, ScopeFolder(scope), dto);

    /// <summary>Writes a list preset into its subfolder of the List scope.</summary>
    public static void SaveList(string name, string subfolder, ListPresetDto dto) =>
        Serialize(name, Path.Combine(ScopeFolder(PresetScope.List), subfolder), dto);

    // ---- Condition list presets ----------------------------------------------------------

    /// <summary>The conditions subfolder of the List scope (one folder per bucket).</summary>
    public static string ConditionsFolder((bool Male, bool Adult) bucket) => Path.Combine(FolderConditions, BucketFolderName(bucket));

    private static string CondFolder((bool Male, bool Adult) bucket) => Path.Combine(RootFolder, PresetScope.List.ToString(), FolderConditions, BucketFolderName(bucket));

    // Folder names predate the tuple keys — same age×sex buckets as the settings themselves.
    private static string BucketFolderName((bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => "AdultMale",
        (true, false) => "YoungMale",
        (false, true) => "AdultFemale",
        _ => "YoungFemale",
    };


    // ---- Read (dispatch by root: the current formats, or the version-1 machinery) ---------

    /// <summary>The full preset; the version-1 unified format migrates into the model.</summary>
    public static AllPresetDto? ReadAll(PresetEntry entry)
    {
        var text = Store.ReadText(entry.path);

        return PresetSerializer.RootElementOf(text) switch
        {
            PresetSerializer.AllPresetRoot => PresetSerializer.DeserializePreset<AllPresetDto>(text),
            _ => AllPresetDto.FromV1(PresetSerializer.ReadPreset(text)),
        };
    }

    /// <summary>The preset's kind slice: Kind-scope presets carry the single kind, All-scope
    /// presets the slice matching the target def. The caller applies it to the settings itself.</summary>
    public static KindPresetDto? ReadKind(PresetEntry entry, ThingDef targetDef)
    {
        var text = Store.ReadText(entry.path);

        if (PresetSerializer.RootElementOf(text) == PresetSerializer.KindPresetRoot)
        {
            return PresetSerializer.DeserializePreset<KindPresetDto>(text);
        }

        var dto = PresetSerializer.ReadPreset(text);

        if (dto?.Kinds == null || dto.Kinds.Count == 0)
        {
            return null;
        }

        if (entry.scope == PresetScope.All)
        {
            // Find the slice for the target kind; if absent, nothing applies.
            return dto.Kinds.FirstOrDefault(k => k.animal == targetDef.defName);
        }

        // Kind- and List-scope presets store a single kind whose defName is the SOURCE kind —
        // on apply we ignore it and use the list/settings for the TARGET kind.
        return dto.Kinds[0];
    }

    /// <summary>A rule list of a List-scope preset, or the list slice of a Kind/All preset
    /// (cross-scope import); the version-1 formats migrate into the model. The payload is
    /// opaque — the owning settings class reads it. Null when nothing readable.</summary>
    public static ListPresetDto? ReadList(PresetEntry entry)
    {
        var text = Store.ReadText(entry.path);

        switch (PresetSerializer.RootElementOf(text))
        {
            case PresetSerializer.ListPresetRoot:
                return PresetSerializer.DeserializePreset<ListPresetDto>(text);
            case PresetSerializer.KindPresetRoot:
                return FromSlice(PresetSerializer.DeserializePreset<KindPresetDto>(text));
            case PresetSerializer.AllPresetRoot:
                return FromSlice(PresetSerializer.DeserializePreset<AllPresetDto>(text)?.Kinds?.FirstOrDefault());
            case PresetSerializer.PresetRoot:
                return FromSlice(PresetSerializer.ReadPreset(text)?.Kinds?.FirstOrDefault());
            case PresetSerializer.RulePresetRoot:
                return FromRules(V1Rules(PresetSerializer.ReadRulePreset(text)));
            default:
#pragma warning disable CS0618
                return FromRules(LegacyPresets.TryReadConditions(entry));
#pragma warning restore CS0618
        }
    }

    // The list payload of a v1 kind slice: the first non-empty rule bucket, or the raw
    // integration sections (a special-settings list).
    private static ListPresetDto? FromSlice(KindPresetDto? kd)
    {
        if (kd == null)
        {
            return null;
        }

        var bucket = new[] { (true, true), (true, false), (false, true), (false, false) }
            .Select(kd.BucketOf).FirstOrDefault(el => el?.HasChildNodes == true);

        if (bucket != null)
        {
            return new ListPresetDto { Content = [bucket] };
        }

        return kd.IntegrationSections.Count > 0 ? new ListPresetDto { Content = [.. kd.IntegrationSections] } : null;
    }

#pragma warning disable CS0618
    // The pre-unification RulePresetDto format still yields attribute-form rules.
    private static ListPresetDto? FromRules(List<BasePriorityRule>? rules)
    {
        if (rules == null)
        {
            return null;
        }

        var dto = new ListPresetDto();
        PriorityRuleXml.WriteRulesInto(dto, rules);
        return dto;
    }

    private static List<BasePriorityRule>? V1Rules(List<PriorityRuleDto>? dtos) =>
        dtos?.Select(r => r.ToRule()).Where(r => r != null).ToList();
#pragma warning restore CS0618

    public static List<PresetEntry> ListConditionPresets()
    {
        var result = new List<PresetEntry>();

        void AddFolder(string dir, (bool Male, bool Adult)? bucket)
        {
            foreach (var p in Store.GetFiles(dir))
            {
                result.Add(new PresetEntry { name = Path.GetFileNameWithoutExtension(p), path = p, date = Store.GetLastWriteTime(p), scope = PresetScope.List, condBucket = bucket });
            }
        }

        // Every condition bucket (cross-bucket import).
        AddFolder(CondFolder((true, true)), (true, true));
        AddFolder(CondFolder((true, false)), (true, false));
        AddFolder(CondFolder((false, true)), (false, true));
        AddFolder(CondFolder((false, false)), (false, false));

        return result.OrderByDescending(e => e.date).ThenBy(e => e.name).ToList();
    }

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
