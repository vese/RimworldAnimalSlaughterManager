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

    // ---- Export --------------------------------------------------------------------------

    public static void ExportAll(string name, ISettingsProvider comp)
    {
        Serialize(name, ScopeFolder(PresetScope.All), PresetDto.From(comp.AllKindSettings));
    }

    public static void ExportKind(string name, ThingDef animalDef, KindSettings ks)
    {
        var dto = new PresetDto();
        dto.Kinds.Add(KindDto.Capture(animalDef, ks));
        Serialize(name, ScopeFolder(PresetScope.Kind), dto);
    }

    public static void ExportList(string name, ThingDef animalDef, IPresettableRuleSet ruleSet)
    {
        var folder = Path.Combine(ScopeFolder(PresetScope.List), ruleSet.PresetsFolder);

        var full = new KindDto { animal = animalDef.defName };

        // The rule set writes its element into a detached holder — move it into the slice.
        var holder = PresetSectionSerialization.Holder();
        ruleSet.WriteXml(holder);
        full.IntegrationSections.AddRange(PresetSectionSerialization.Children(holder));

        var dto = new PresetDto();
        dto.Kinds.Add(full);
        Serialize(name, folder, dto);
    }

    // ---- Condition list presets ----------------------------------------------------------

    private static string CondFolder((bool Male, bool Adult) bucket) => Path.Combine(RootFolder, PresetScope.List.ToString(), FolderConditions, BucketFolderName(bucket));

    // Folder names predate the tuple keys — same age×sex buckets as the settings themselves.
    private static string BucketFolderName((bool Male, bool Adult) bucket) => bucket switch
    {
        (true, true) => "AdultMale",
        (true, false) => "YoungMale",
        (false, true) => "AdultFemale",
        _ => "YoungFemale",
    };

    // A condition-list preset rides the common PresetDto format: one KindDto with
    // the rules in the bucket's own list. The bucket is the preset's ORIGIN — on apply the
    // rules go to whatever bucket is targeted (cross-bucket import).
    public static void ExportConditions(string name, (bool Male, bool Adult) bucket, List<BasePriorityRule> rules)
    {

        var kd = new KindDto { animal = "" };
        kd.GetBucketRules(bucket).AddRange(rules.Select(PriorityRuleDto.From));

        var dto = new PresetDto();
        dto.Kinds.Add(kd);
        Serialize(name, CondFolder(bucket), dto);
    }

    /// <summary>Condition rules of a List-scope preset, any of the three historical formats
    /// (current format / pre-unification RulePresetDto / legacy SlaughterCondition list).
    /// Null when nothing readable.</summary>
    public static List<BasePriorityRule>? ApplyConditions(PresetEntry entry)
    {
        var text = Store.ReadText(entry.path);

        switch (PresetSerializer.RootElementOf(text))
        {
            case PresetSerializer.PresetRoot:
                var kd = PresetSerializer.ReadPreset(text)?.Kinds?.FirstOrDefault();
                return kd == null ? null : RulesOf(kd.GetConditionRules());
            case PresetSerializer.RulePresetRoot:
                return RulesOf(PresetSerializer.ReadRulePreset(text));
            default:
#pragma warning disable CS0618
                return LegacyPresets.TryReadConditions(entry);
#pragma warning restore CS0618
        }
    }

    private static List<BasePriorityRule>? RulesOf(List<PriorityRuleDto>? dtos) =>
        dtos?.Select(r => r.ToRule()).Where(r => r != null).ToList();

    /// <summary>Rules of one bucket taken from a Kind/All preset (the source kind's defName
    /// is ignored — the slice applies to any target kind). Null when there is no such slice.</summary>
    public static List<BasePriorityRule>? ReadKindRules(PresetEntry entry, (bool Male, bool Adult) bucket)
    {
        var kd = PresetSerializer.ReadPreset(Store.ReadText(entry.path))?.Kinds?.FirstOrDefault();

        if (kd == null)
        {
            return null;
        }

        return RulesOf(kd.GetBucketRules(bucket));
    }

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

    // ---- Reading for the callers to apply themselves -------------------------------------

    /// <summary>The whole All-scope preset; the caller applies it to the map itself.</summary>
    public static PresetDto? ReadPreset(PresetEntry entry)
    {
        return PresetSerializer.ReadPreset(Store.ReadText(entry.path));
    }

    /// <summary>The preset's kind slice: Kind-scope presets carry the single kind, All-scope
    /// presets the slice matching the target def. The caller applies it to the settings itself.</summary>
    public static KindDto? ReadKindSlice(PresetEntry entry, ThingDef targetDef)
    {
        return RelevantKindDto(entry, targetDef);
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

    private static KindDto? RelevantKindDto(PresetEntry? entry, ThingDef targetDef)
    {
        var dto = PresetSerializer.ReadPreset(entry == null ? null : Store.ReadText(entry.path));
        if (dto?.Kinds == null || dto.Kinds.Count == 0)
        {
            return null;
        }
        if (entry!.scope == PresetScope.All)
        {
            // Find the slice for the target kind; if absent, nothing applies.
            return dto.Kinds.FirstOrDefault(k => k.animal == targetDef.defName);
        }
        // Kind- and List-scope presets store a single kind whose defName is the SOURCE kind —
        // on apply we ignore it and use the list/settings for the TARGET kind.
        return dto.Kinds[0];
    }

    private static void Serialize(string name, string folder, PresetDto dto)
    {
        Store.WriteText(Path.Combine(folder, Sanitize(name) + ".xml"), PresetSerializer.SerializeToText(dto));
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
