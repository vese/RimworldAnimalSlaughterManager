using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ASM;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM
{
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
    /// <item><description><c>List/&lt;Keep|Cull|Spare|ForceCull&gt;/</c> — a single trait list for
    /// one kind (save a list, load the corresponding list into another kind).</description></item>
    /// </list>
    /// A higher-scope preset can be applied to a narrower target (All→kind, All→list, kind→list):
    /// only the target's slice is taken. Higher-scope presets cannot be deleted from a narrower
    /// context (see <see cref="PresetEntry.DeletableFrom"/>).
    /// </summary>
    public static class PresetIO
    {
        private static string RootFolder => Path.Combine(Application.persistentDataPath, "AnimalSlaughterManagerPresets");

        private static string ScopeFolder(PresetScope scope)
        {
            switch (scope)
            {
                case PresetScope.All: return Path.Combine(RootFolder, "All");
                case PresetScope.Kind: return Path.Combine(RootFolder, "Kind");
                default: return Path.Combine(RootFolder, "List");
            }
        }

        /// <summary>Presets of one scope (List covers every rule-set subfolder), newest-first by file write time.</summary>
        public static List<PresetEntry> ListPresets(PresetScope scope)
        {
            var result = new List<PresetEntry>();
            void AddFolder(string dir)
            {
                if (!Directory.Exists(dir)) return;
                foreach (var p in Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly))
                {
                    result.Add(new PresetEntry
                    {
                        name = Path.GetFileNameWithoutExtension(p),
                        path = p,
                        date = File.GetLastWriteTime(p),
                        scope = scope
                    });
                }
            }

            if (scope == PresetScope.List)
            {
                AddFolder(Path.Combine(ScopeFolder(scope), "Keep"));
                AddFolder(Path.Combine(ScopeFolder(scope), "ForceCull"));
            }
            else
            {
                AddFolder(ScopeFolder(scope));
            }

            // Legacy: presets saved by older versions live in the root folder and are all-animals.
            if (scope == PresetScope.All)
                AddFolder(RootFolder);
            return result.OrderByDescending(e => e.date).ThenBy(e => e.name).ToList();
        }

        // ---- Export --------------------------------------------------------------------------

        public static void ExportAll(string name, ASM_MapComp comp)
        {
            Directory.CreateDirectory(ScopeFolder(PresetScope.All));
            Serialize(name, ScopeFolder(PresetScope.All), SlaughterPresetDto.From(comp));
        }

        public static void ExportKind(string name, ThingDef animalDef, KindSettings ks)
        {
            Directory.CreateDirectory(ScopeFolder(PresetScope.Kind));
            var dto = new SlaughterPresetDto();
            dto.Kinds.Add(KindDto.From(animalDef, ks));
            Serialize(name, ScopeFolder(PresetScope.Kind), dto);
        }

        public static void ExportList(string name, ThingDef animalDef, IPresettableRuleSet ruleSet)
        {
            var folder = Path.Combine(ScopeFolder(PresetScope.List), ruleSet.PresetsFolder);
            Directory.CreateDirectory(folder);

            var full = new KindDto { animal = animalDef.defName };
            ruleSet.Save(full);

            var dto = new SlaughterPresetDto();
            dto.Kinds.Add(full);
            Serialize(name, folder, dto);
        }

        // ---- Condition list presets ----------------------------------------------------------

        private static string CondFolder(CondBucket bucket) => Path.Combine(RootFolder, "List", "Conditions", bucket.ToString());

        public static void ExportConditions(string name, CondBucket bucket, List<BasePriorityRule> rules)
        {
            Directory.CreateDirectory(CondFolder(bucket));
            var dto = new RulePresetDto { bucket = bucket.ToString(), rules = rules.Select(PriorityRuleDto.From).ToList() };
            var ser = new XmlSerializer(typeof(RulePresetDto));
            using (var w = new StreamWriter(Path.Combine(CondFolder(bucket), Sanitize(name) + ".xml")))
                ser.Serialize(w, dto);
        }

        public static List<BasePriorityRule> ApplyConditions(PresetEntry entry)
        {
            try
            {
                var ser = new XmlSerializer(typeof(RulePresetDto));
                using (var r = new StreamReader(entry.path))
                {
                    var dto = (RulePresetDto)ser.Deserialize(r);
                    return dto.rules.Select(r => r.ToRule()).Where(r => r != null).ToList();
                }
            }
            catch
            {
                return ReadLegacyConditions(entry);
            }
        }

        // Presets saved before the priority-rule refactor stored SlaughterCondition lists.
        private static List<BasePriorityRule> ReadLegacyConditions(PresetEntry entry)
        {
            try
            {
                var ser = new XmlSerializer(typeof(ConditionPresetDto));
                using (var r = new StreamReader(entry.path))
                {
                    var dto = (ConditionPresetDto)ser.Deserialize(r);
#pragma warning disable CS0618
                    return dto.conditions.Select(c => KindPrioritySettingsLegacy.Convert(c.ToCondition())).Where(c => c != null).ToList();
#pragma warning restore CS0618
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Rules of one bucket taken from a Kind/All preset (the source kind's defName
        /// is ignored — the slice applies to any target kind). Null when there is no such slice.</summary>
        public static List<BasePriorityRule>? ReadKindRules(PresetEntry entry, CondBucket bucket)
        {
            var kd = LoadDto(entry)?.Kinds?.FirstOrDefault();

            if (kd == null)
            {
                return null;
            }

            var rules = bucket switch
            {
                CondBucket.AdultMale => kd.PrioRulesAdultMale,
                CondBucket.YoungMale => kd.PrioRulesYoungMale,
                CondBucket.AdultFemale => kd.PrioRulesAdultFemale,
                _ => kd.PrioRulesYoungFemale,
            };

            return rules?.Select(r => r.ToRule()).Where(r => r != null).ToList();
        }

        public static List<PresetEntry> ListConditionPresets(CondBucket bucket)
        {
            var result = new List<PresetEntry>();
            void AddFolder(string dir, CondBucket? b)
            {
                if (!Directory.Exists(dir)) return;
                foreach (var p in Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly))
                {
                    var pe = new PresetEntry { name = Path.GetFileNameWithoutExtension(p), path = p, date = File.GetLastWriteTime(p), scope = PresetScope.List, condBucket = b };
                    result.Add(pe);
                }
            }
            // Own bucket + all other condition buckets (cross-bucket import).
            foreach (CondBucket b in System.Enum.GetValues(typeof(CondBucket)))
                AddFolder(CondFolder(b), b);
            return result.OrderByDescending(e => e.date).ThenBy(e => e.name).ToList();
        }

        // ---- Apply ---------------------------------------------------------------------------

        /// <summary>Apply a whole all-animals preset to the map (all configured kinds).</summary>
        public static void ApplyAll(PresetEntry entry, ASM_MapComp comp)
        {
            var dto = LoadDto(entry);
            if (dto == null) return;
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
            if (kd == null) return false;
            kd.ApplyTo(targetKs);
            return true;
        }

        /// <summary>Apply a preset's rules into <paramref name="ruleSet"/>. Works for List-scope
        /// presets (their rule list), and Kind/All presets (the slice for <paramref name="targetDef"/>).
        /// Returns false when the preset has no slice for this kind.</summary>
        public static bool ApplyList(PresetEntry entry, ThingDef targetDef, IPresettableRuleSet ruleSet)
        {
            var kd = RelevantKindDto(entry, targetDef);
            if (kd == null) return false;
            return ruleSet.Load(kd);
        }

        public static void Delete(PresetEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.path)) return;
            if (File.Exists(entry.path)) File.Delete(entry.path);
        }

        // ---- Helpers -------------------------------------------------------------------------

        private static KindDto RelevantKindDto(PresetEntry entry, ThingDef targetDef)
        {
            var dto = LoadDto(entry);
            if (dto?.Kinds == null || dto.Kinds.Count == 0) return null;
            if (entry.scope == PresetScope.All)
            {
                // Find the slice for the target kind; if absent, nothing applies.
                return dto.Kinds.FirstOrDefault(k => k.animal == targetDef.defName);
            }
            // Kind- and List-scope presets store a single kind whose defName is the SOURCE kind —
            // on apply we ignore it and use the list/settings for the TARGET kind.
            return dto.Kinds[0];
        }

        private static SlaughterPresetDto LoadDto(PresetEntry entry)
        {
            if (entry == null || !File.Exists(entry.path)) return null;
            try
            {
                var ser = new XmlSerializer(typeof(SlaughterPresetDto));
                using (var r = new StreamReader(entry.path))
                    return (SlaughterPresetDto)ser.Deserialize(r);
            }
            catch (Exception e)
            {
                Log.Error($"[ASM] failed to read preset '{entry.name}' from {entry.path}: {e.Message}");
                return null;
            }
        }

        private static void Serialize(string name, string folder, SlaughterPresetDto dto)
        {
            var ser = new XmlSerializer(typeof(SlaughterPresetDto));
            using (var w = new StreamWriter(Path.Combine(folder, Sanitize(name) + ".xml")))
                ser.Serialize(w, dto);
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }
    }

    /// <summary>Preset breadth: All-animals &gt; single Kind &gt; single List.</summary>
    public enum PresetScope { All, Kind, List }

    /// <summary>Which of the four trait lists a List-scope preset holds.</summary>
    /// <summary>Which of the four condition buckets a List-scope condition preset holds.</summary>
    public enum CondBucket { AdultMale, YoungMale, AdultFemale, YoungFemale }

}
