using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ASM;
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

        private static string ScopeFolder(PresetScope scope, TraitListKind listKind = TraitListKind.Keep)
        {
            switch (scope)
            {
                case PresetScope.All: return Path.Combine(RootFolder, "All");
                case PresetScope.Kind: return Path.Combine(RootFolder, "Kind");
                default: return Path.Combine(RootFolder, "List", listKind.ToString());
            }
        }

        /// <summary>Presets of one scope (+list), newest-first by file write time.</summary>
        public static List<PresetEntry> ListPresets(PresetScope scope, TraitListKind listKind = TraitListKind.Keep)
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
                        scope = scope,
                        listKind = (scope == PresetScope.List) ? listKind : (TraitListKind?)null
                    });
                }
            }
            AddFolder(ScopeFolder(scope, listKind));
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

        public static void ExportList(string name, TraitListKind listKind, ThingDef animalDef, IList list)
        {
            Directory.CreateDirectory(ScopeFolder(PresetScope.List, listKind));
            Serialize(name, ScopeFolder(PresetScope.List, listKind), ListDto(animalDef, listKind, list));
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

        /// <summary>Apply a preset's <paramref name="listKind"/> list into <paramref name="targetList"/>
        /// (cleared first). Works for List-scope (its list), Kind-scope (that kind's list), and
        /// All-scope (the slice for <paramref name="targetDef"/>'s list). Returns false if nothing applied.</summary>
        public static bool ApplyList(PresetEntry entry, TraitListKind listKind, ThingDef targetDef, IList targetList)
        {
            var kd = RelevantKindDto(entry, targetDef);
            if (kd == null) return false;
            targetList.Clear();
            foreach (var o in BuildList(kd, listKind))
                targetList.Add(o);
            return true;
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
            catch { return null; }
        }

        private static void Serialize(string name, string folder, SlaughterPresetDto dto)
        {
            var ser = new XmlSerializer(typeof(SlaughterPresetDto));
            using (var w = new StreamWriter(Path.Combine(folder, Sanitize(name) + ".xml")))
                ser.Serialize(w, dto);
        }

        private static SlaughterPresetDto ListDto(ThingDef animalDef, TraitListKind listKind, IList list)
        {
            // A List-scope preset is a single-kind DTO carrying only one populated list.
            var full = KindDto.From(animalDef, new KindSettings());
            switch (listKind)
            {
                case TraitListKind.Keep: full.KeepTraits = TraitList(list, true); break;
                case TraitListKind.Cull: full.CullTraits = TraitList(list, false); break;
                case TraitListKind.Spare: full.SpareTraits = TraitList(list, false); break;
                case TraitListKind.ForceCull: full.ForceCullTraits = TraitList(list, false); break;
            }
            var dto = new SlaughterPresetDto();
            dto.Kinds.Add(full);
            return dto;
        }

        private static List<TraitDto> TraitList(IList list, bool isKeep)
        {
            var result = new List<TraitDto>();
            foreach (var item in list)
            {
                if (isKeep && item is TraitProtectRule tt)
                    result.Add(TraitDto.From(tt));
                else if (!isKeep && item is TraitRule ct)
                    result.Add(TraitDto.From(ct));
            }
            return result;
        }

        private static IList BuildList(KindDto kd, TraitListKind listKind)
        {
            switch (listKind)
            {
                case TraitListKind.Keep: return kd.KeepTraits.Select(t => t.ToTarget()).Where(t => t != null).ToList();
                case TraitListKind.Cull: return kd.CullTraits.Select(t => t.ToCull()).Where(c => c != null).ToList();
                case TraitListKind.Spare: return kd.SpareTraits.Select(t => t.ToCull()).Where(c => c != null).ToList();
                default: return kd.ForceCullTraits.Select(t => t.ToCull()).Where(c => c != null).ToList();
            }
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
    public enum TraitListKind { Keep, Cull, Spare, ForceCull }

    /// <summary>Which of the four condition buckets a List-scope condition preset holds.</summary>
    public enum CondBucket { AdultMale, YoungMale, AdultFemale, YoungFemale }

    public class PresetEntry
    {
        public string name;
        public string path;
        public DateTime date;
        public PresetScope scope;
        public TraitListKind? listKind;
        public CondBucket? condBucket;

        /// <summary>Rank for delete-gating: a context may only delete presets at or below its own rank.</summary>
        public int Rank => scope == PresetScope.All ? 3 : scope == PresetScope.Kind ? 2 : 1;

        /// <summary>True if a management context of <paramref name="contextRank"/> may delete this preset.</summary>
        public bool DeletableFrom(int contextRank) => Rank <= contextRank;
    }

    [XmlRoot("SlaughterPreset")]
    public class SlaughterPresetDto
    {
        [XmlElement("Kind")]
        public List<KindDto> Kinds = new List<KindDto>();

        public static SlaughterPresetDto From(ASM_MapComp comp)
        {
            var dto = new SlaughterPresetDto();
            foreach (var kv in comp.kindSettings)
            {
                if (kv.Value == null || !kv.Value.Customized) continue;
                dto.Kinds.Add(KindDto.From(kv.Key, kv.Value));
            }
            return dto;
        }

        public void ApplyTo(ASM_MapComp comp)
        {
            foreach (var kd in Kinds)
            {
                var def = DefDatabase<ThingDef>.GetNamed(kd.animal, false);
                if (def == null) continue;
                kd.ApplyTo(comp.GetSettings(def));
            }
        }
    }

    public class KindDto
    {
        [XmlAttribute] public string animal;
        public string malePref;
        public string femalePref;
        public string maleYoungPref;
        public string femaleYoungPref;
        [XmlArray("KeepTraits")] [XmlArrayItem("Trait")]
        public List<TraitDto> KeepTraits = new List<TraitDto>();
        [XmlArray("CullTraits")] [XmlArrayItem("Trait")]
        public List<TraitDto> CullTraits = new List<TraitDto>();
        [XmlArray("SpareTraits")] [XmlArrayItem("Trait")]
        public List<TraitDto> SpareTraits = new List<TraitDto>();
        [XmlArray("ForceCullTraits")] [XmlArrayItem("Trait")]
        public List<TraitDto> ForceCullTraits = new List<TraitDto>();
        [XmlArray("PrioAdultMale")] [XmlArrayItem("Cond")]
        public List<ConditionDto> PrioAdultMale;
        [XmlArray("PrioYoungMale")] [XmlArrayItem("Cond")]
        public List<ConditionDto> PrioYoungMale;
        [XmlArray("PrioAdultFemale")] [XmlArrayItem("Cond")]
        public List<ConditionDto> PrioAdultFemale;
        [XmlArray("PrioYoungFemale")] [XmlArrayItem("Cond")]
        public List<ConditionDto> PrioYoungFemale;
        [XmlArray("PrioRulesAdultMale")] [XmlArrayItem("Rule")]
        public List<PriorityRuleDto> PrioRulesAdultMale = new List<PriorityRuleDto>();
        [XmlArray("PrioRulesYoungMale")] [XmlArrayItem("Rule")]
        public List<PriorityRuleDto> PrioRulesYoungMale = new List<PriorityRuleDto>();
        [XmlArray("PrioRulesAdultFemale")] [XmlArrayItem("Rule")]
        public List<PriorityRuleDto> PrioRulesAdultFemale = new List<PriorityRuleDto>();
        [XmlArray("PrioRulesYoungFemale")] [XmlArrayItem("Rule")]
        public List<PriorityRuleDto> PrioRulesYoungFemale = new List<PriorityRuleDto>();

        public static KindDto From(ThingDef def, KindSettings k)
        {
            var dto = new KindDto { animal = def.defName };
            k.Save(dto);
            return dto;
        }

        public void ApplyTo(KindSettings ks)
        {
            ks.Reset();
            ks.Load(this);
        }
    }

    public class TraitDto
    {
        [XmlAttribute] public string trait;
        [XmlAttribute] public int keepCount = 1;
        [XmlAttribute] public string ageScope = "Both";
        [XmlAttribute] public string genderScope = "Any";
        [XmlAttribute] public string inheritMode = "Both";

        public static TraitDto From(TraitProtectRule t) => new TraitDto
        {
            trait = t.trait?.defName,
            keepCount = t.keepCount,
            ageScope = t.ageScope.ToString(),
            genderScope = t.genderScope.ToString(),
            inheritMode = t.inheritMode.ToString()
        };

        public static TraitDto From(TraitRule c) => new TraitDto
        {
            trait = c.trait?.defName,
            ageScope = c.ageScope.ToString(),
            genderScope = c.genderScope.ToString(),
            inheritMode = c.inheritMode.ToString()
        };

        public TraitProtectRule ToTarget()
        {
            var h = DefDatabase<HediffDef>.GetNamed(trait, false);
            if (h == null) return null;
            var t = new TraitProtectRule(h) { keepCount = keepCount };
            Enum.TryParse(ageScope, out t.ageScope);
            Enum.TryParse(genderScope, out t.genderScope);
            Enum.TryParse(inheritMode, out t.inheritMode);
            return t;
        }

        public TraitRule ToCull()
        {
            var h = DefDatabase<HediffDef>.GetNamed(trait, false);
            if (h == null) return null;
            var c = new TraitRule(h);
            Enum.TryParse(ageScope, out c.ageScope);
            Enum.TryParse(genderScope, out c.genderScope);
            Enum.TryParse(inheritMode, out c.inheritMode);
            return c;
        }
    }

    /// <summary>XML-serializable form of SlaughterCondition for preset files (stores defNames).</summary>
    public class ConditionDto
    {
        [XmlAttribute] public string type;
        [XmlAttribute] public bool has = true;
        [XmlAttribute] public string trait;
        [XmlAttribute] public string disease;
        [XmlAttribute] public string trainable;
        [XmlAttribute] public string inheritMode;

        public static ConditionDto From(SlaughterCondition c) => new ConditionDto
        {
            type = c.type.ToString(),
            has = c.has,
            trait = c.trait?.defName,
            disease = c.disease?.defName,
            trainable = c.trainable?.defName,
            inheritMode = c.inheritMode.ToString()
        };

        public SlaughterCondition ToCondition()
        {
            if (!Enum.TryParse(type, out CondType t)) return new SlaughterCondition(CondType.Pregnancy);
            return new SlaughterCondition(t)
            {
                has = has,
                trait = string.IsNullOrEmpty(trait) ? null : DefDatabase<HediffDef>.GetNamedSilentFail(trait),
                disease = string.IsNullOrEmpty(disease) ? null : DefDatabase<HediffDef>.GetNamedSilentFail(disease),
                trainable = string.IsNullOrEmpty(trainable) ? null : DefDatabase<TrainableDef>.GetNamedSilentFail(trainable),
                inheritMode = string.IsNullOrEmpty(inheritMode) ? TraitInheritability.Both : (TraitInheritability)Enum.Parse(typeof(TraitInheritability), inheritMode)
            };
        }
    }

    [XmlRoot("ConditionPreset")]
    public class ConditionPresetDto
    {
        [XmlAttribute] public string bucket;
        [XmlElement("Cond")] public List<ConditionDto> conditions = new List<ConditionDto>();
    }

    /// <summary>XML-serializable form of a priority-rule list preset (stores defNames).</summary>
    public class RulePresetDto
    {
        [XmlAttribute] public string bucket;
        [XmlElement("Rule")] public List<PriorityRuleDto> rules = new List<PriorityRuleDto>();
    }

    /// <summary>XML form of one BasePriorityRule: the rule type name plus its parameters.</summary>
    public class PriorityRuleDto
    {
        private const string Pregnancy = nameof(PregnancyPriorityRule);
        private const string Bond = nameof(BondPriorityRule);
        private const string DiseaseAny = nameof(DiseaseAnyPriorityRule);
        private const string Disease = nameof(DiseasePriorityRule);
        private const string Training = nameof(TrainingPriorityRule);
        private const string TrainingGeneral = nameof(TrainingGeneralPriorityRule);
        private const string Trait = nameof(TraitPriorityRule);
        private const string TraitGeneral = nameof(TraitGeneralPriorityRule);

        [XmlAttribute] public string rule;
        [XmlAttribute] public bool has = true;
        [XmlAttribute] public string trait;
        [XmlAttribute] public string disease;
        [XmlAttribute] public string trainable;
        [XmlAttribute] public string generalType;
        [XmlAttribute] public string inheritability;

        public static PriorityRuleDto From(BasePriorityRule r) => r switch
        {
            PregnancyPriorityRule x => new PriorityRuleDto { rule = Pregnancy, has = x.has },
            BondPriorityRule x => new PriorityRuleDto { rule = Bond, has = x.has },
            DiseaseAnyPriorityRule x => new PriorityRuleDto { rule = DiseaseAny, has = x.has },
            DiseasePriorityRule x => new PriorityRuleDto { rule = Disease, has = x.has, disease = x.disease?.defName },
            TrainingPriorityRule x => new PriorityRuleDto { rule = Training, has = x.has, trainable = x.trainable?.defName },
            TrainingGeneralPriorityRule x => new PriorityRuleDto { rule = TrainingGeneral, generalType = x.type.ToString() },
            TraitPriorityRule x => new PriorityRuleDto { rule = Trait, has = x.has, trait = x.trait?.defName, inheritability = x.inheritability.ToString() },
            TraitGeneralPriorityRule x => new PriorityRuleDto { rule = TraitGeneral, has = x.has, generalType = x.type.ToString(), inheritability = x.inheritability.ToString() },
            _ => new PriorityRuleDto { rule = "" },
        };

        public BasePriorityRule ToRule()
        {
            switch (rule)
            {
                case Pregnancy:
                    return new PregnancyPriorityRule { has = has };
                case Bond:
                    return new BondPriorityRule { has = has };
                case DiseaseAny:
                    return new DiseaseAnyPriorityRule { has = has };
                case Disease:
                    return new DiseasePriorityRule { has = has, disease = DefDatabase<HediffDef>.GetNamedSilentFail(disease ?? "") };
                case Training:
                    return new TrainingPriorityRule { has = has, trainable = DefDatabase<TrainableDef>.GetNamedSilentFail(trainable ?? "") };
                case TrainingGeneral:
                    return Enum.TryParse(generalType, out TrainingGeneralType tg)
                        ? new TrainingGeneralPriorityRule { type = tg }
                        : null;
                case Trait:
                    return new TraitPriorityRule
                    {
                        has = has,
                        trait = DefDatabase<HediffDef>.GetNamedSilentFail(trait ?? ""),
                        inheritability = ParseInheritability(),
                    };
                case TraitGeneral:
                    return Enum.TryParse(generalType, out TraitType tt)
                        ? new TraitGeneralPriorityRule { has = has, type = tt, inheritability = ParseInheritability() }
                        : null;
                default:
                    return null;
            }
        }

        private TraitInheritability ParseInheritability() =>
            Enum.TryParse(inheritability, out TraitInheritability ti) ? ti : TraitInheritability.Both;
    }
}
