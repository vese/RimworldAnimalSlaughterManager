using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using RimWorld;
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
        private const string FolderAll = "All";
        private const string FolderKind = "Kind";
        private const string FolderList = "List";
        private const string FolderKeep = "Keep";
        private const string FolderForceCull = "ForceCull";
        private const string FolderConditions = "Conditions";
        /// <summary>File access of the preset subsystem; tests may substitute their own store.</summary>
        internal static IPresetStore Store { get; set; } = new FilePresetStore();

        /// <summary>Tests redirect the preset tree here instead of the game's persistentDataPath.</summary>
        internal static string? RootFolderOverride { get; set; }

        internal static string RootFolder => RootFolderOverride ?? GameRoot();

        // Kept apart so that RootFolder never touches UnityEngine unless the game really asks.
        private static string GameRoot() => Path.Combine(Application.persistentDataPath, RootFolderName);

        private static string ScopeFolder(PresetScope scope)
        {
            switch (scope)
            {
                case PresetScope.All: return Path.Combine(RootFolder, FolderAll);
                case PresetScope.Kind: return Path.Combine(RootFolder, FolderKind);
                default: return Path.Combine(RootFolder, FolderList);
            }
        }

        /// <summary>Presets of one scope (List covers every rule-set subfolder), newest-first by file write time.</summary>
        public static List<PresetEntry> ListPresets(PresetScope scope)
        {
            var result = new List<PresetEntry>();
            void AddFolder(string dir)
            {
                foreach (var p in Store.Files(dir))
                {
                    result.Add(new PresetEntry
                    {
                        name = Path.GetFileNameWithoutExtension(p),
                        path = p,
                        date = Store.LastWrite(p),
                        scope = scope
                    });
                }
            }

            if (scope == PresetScope.List)
            {
                AddFolder(Path.Combine(ScopeFolder(scope), FolderKeep));
                AddFolder(Path.Combine(ScopeFolder(scope), FolderForceCull));
            }
            else
            {
                AddFolder(ScopeFolder(scope));
            }

            // Presets saved before scopes existed live in the root folder.
            if (scope == PresetScope.All)
            {
                PresetLegacyReader.AddRootPresets(result);
            }

            return result.OrderByDescending(e => e.date).ThenBy(e => e.name).ToList();
        }

        // ---- Export --------------------------------------------------------------------------

        public static void ExportAll(string name, ASM_MapComp comp)
        {
            Store.EnsureFolder(ScopeFolder(PresetScope.All));
            Serialize(name, ScopeFolder(PresetScope.All), SlaughterPresetDto.From(comp));
        }

        public static void ExportKind(string name, ThingDef animalDef, KindSettings ks)
        {
            Store.EnsureFolder(ScopeFolder(PresetScope.Kind));
            var dto = new SlaughterPresetDto();
            dto.Kinds.Add(KindDto.From(animalDef, ks));
            Serialize(name, ScopeFolder(PresetScope.Kind), dto);
        }

        public static void ExportList(string name, ThingDef animalDef, IPresettableRuleSet ruleSet)
        {
            var folder = Path.Combine(ScopeFolder(PresetScope.List), ruleSet.PresetsFolder);
            Store.EnsureFolder(folder);

            var full = new KindDto { animal = animalDef.defName };
            ruleSet.Save(full);

            var dto = new SlaughterPresetDto();
            dto.Kinds.Add(full);
            Serialize(name, folder, dto);
        }

        // ---- Condition list presets ----------------------------------------------------------

        private static string CondFolder(CondBucket bucket) => Path.Combine(RootFolder, FolderList, FolderConditions, bucket.ToString());

        // A condition-list preset rides the common SlaughterPresetDto carrier: one KindDto with
        // the rules in the bucket's own list. The bucket is the preset's ORIGIN — on apply the
        // rules go to whatever bucket is targeted (cross-bucket import).
        public static void ExportConditions(string name, CondBucket bucket, List<BasePriorityRule> rules)
        {
            Store.EnsureFolder(CondFolder(bucket));

            var kd = new KindDto { animal = "" };
            kd.BucketRules(bucket).AddRange(rules.Select(PriorityRuleDto.From));

            var dto = new SlaughterPresetDto();
            dto.Kinds.Add(kd);
            Serialize(name, CondFolder(bucket), dto);
        }

        /// <summary>Condition rules of a List-scope preset, any of the three historical formats
        /// (unified carrier / pre-unification RulePresetDto / legacy SlaughterCondition list).
        /// Null when nothing readable.</summary>
        public static List<BasePriorityRule>? ApplyConditions(PresetEntry entry)
        {
            var text = Store.ReadText(entry.path);

            switch (PresetSerializer.RootElementOf(text))
            {
                case PresetSerializer.CarrierRoot:
                    var kd = PresetSerializer.ReadCarrier(text)?.Kinds?.FirstOrDefault();
                    return kd == null ? null : RulesOf(kd.ConditionRules());
                case PresetSerializer.RulePresetRoot:
                    return RulesOf(PresetSerializer.ReadRulePreset(text));
                default:
                    return PresetLegacyReader.TryReadConditions(entry);
            }
        }

        private static List<BasePriorityRule>? RulesOf(List<PriorityRuleDto>? dtos) =>
            dtos?.Select(r => r.ToRule()).Where(r => r != null).ToList();

        /// <summary>Rules of one bucket taken from a Kind/All preset (the source kind's defName
        /// is ignored — the slice applies to any target kind). Null when there is no such slice.</summary>
        public static List<BasePriorityRule>? ReadKindRules(PresetEntry entry, CondBucket bucket)
        {
            var kd = PresetSerializer.ReadCarrier(Store.ReadText(entry.path))?.Kinds?.FirstOrDefault();

            if (kd == null)
            {
                return null;
            }

            return RulesOf(kd.BucketRules(bucket));
        }

        public static List<PresetEntry> ListConditionPresets(CondBucket bucket)
        {
            var result = new List<PresetEntry>();
            void AddFolder(string dir, CondBucket? b)
            {
                foreach (var p in Store.Files(dir))
                {
                    result.Add(new PresetEntry { name = Path.GetFileNameWithoutExtension(p), path = p, date = Store.LastWrite(p), scope = PresetScope.List, condBucket = b });
                }
            }

            // Own bucket + all other condition buckets (cross-bucket import).
            foreach (CondBucket b in Enum.GetValues(typeof(CondBucket)))
            {
                AddFolder(CondFolder(b), b);
            }

            return result.OrderByDescending(e => e.date).ThenBy(e => e.name).ToList();
        }

        // ---- Apply ---------------------------------------------------------------------------

        /// <summary>Apply a whole all-animals preset to the map (all configured kinds).</summary>
        public static void ApplyAll(PresetEntry entry, ASM_MapComp comp)
        {
            var dto = PresetSerializer.ReadCarrier(Store.ReadText(entry.path));
            if (dto == null)
            {
                return;
            }
            dto.ApplyTo(comp);
            comp.MarkDirty();
        }

        /// <summary>Apply a preset's full per-kind settings to <paramref name="targetKs"/>.
        /// Works for Kind-scope presets (the single kind) and All-scope presets (the slice whose
        /// animal matches <paramref name="targetDef"/>). Returns false if an All preset has no
        /// slice for this kind.</summary>
        public static bool ApplyKindSettings(PresetEntry entry, ThingDef targetDef, KindSettings targetKs)
        {
            var kd = RelevantKindDto(entry, targetDef);
            if (kd == null)
            {
                return false;
            }
            kd.ApplyTo(targetKs);
            return true;
        }

        /// <summary>Apply a preset's rules into <paramref name="ruleSet"/>. Works for List-scope
        /// presets (their rule list), and Kind/All presets (the slice for <paramref name="targetDef"/>).
        /// Returns false when the preset has no slice for this kind.</summary>
        public static bool ApplyList(PresetEntry entry, ThingDef targetDef, IPresettableRuleSet ruleSet)
        {
            var kd = RelevantKindDto(entry, targetDef);
            if (kd == null)
            {
                return false;
            }
            return ruleSet.Load(kd);
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
            var dto = PresetSerializer.ReadCarrier(entry == null ? null : Store.ReadText(entry.path));
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

        private static void Serialize(string name, string folder, SlaughterPresetDto dto)
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
