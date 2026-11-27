#pragma warning disable CS0612, CS0618

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Verse;

namespace ASM;

/// <summary>
/// Everything the current preset code needs from the old formats, behind one seam: root-folder
/// presets (saved before scopes existed), condition-list presets (before the priority-rule
/// refactor) and old preset formats. Delete this class together with the rest of the Legacy folder
/// when the save-compatibility horizon allows — the callers are PresetSerializer and
/// PresetRepository only.
/// </summary>
[Obsolete]
public static class LegacyPresets
{
    private static readonly IPresetStore Store = new FilePresetStore();

    /// <summary>Adds root-folder presets to an All-scope listing (they have no scope folder).</summary>
    public static void AddRootPresets(List<PresetEntry> result, string rootFolder)
    {
        foreach (var p in Store.GetFiles(rootFolder))
        {
            result.Add(new PresetEntry
            {
                name = Path.GetFileNameWithoutExtension(p),
                path = p,
                date = Store.GetLastWriteTime(p),
                scope = PresetScope.All
            });
        }
    }

    /// <summary>Condition rules of a legacy condition-preset file; null when the file is not
    /// one (the current-format reader falls back here).</summary>
    public static List<BasePriorityRule>? TryReadConditions(PresetEntry entry)
    {
        var conditions = ReadConditionPreset(Store.ReadText(entry.path));

        if (conditions == null)
        {
            return null;
        }

        var rules = new List<BasePriorityRule>();

        foreach (var c in conditions)
        {
            var rule = KindPrioritySettingsLegacy.Convert(c.ToCondition());

            if (rule != null)
            {
                rules.Add(rule);
            }
        }

        return rules;
    }

    /// <summary>An old-format preset converted into the current one; null when the text is not
    /// an old format (no legacy condition lists anywhere).</summary>
    public static List<KindPreset>? TryReadPreset(string? text)
    {
        if (text == null)
        {
            return null;
        }

        try
        {
            using var reader = new StringReader(text);
            var legacy = (LegacyPresetDto)PresetSerializer.Serializer<LegacyPresetDto>().Deserialize(reader);

            return legacy != null && legacy.HasConditions ? legacy.ToCurrent() : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The rules of the pre-unification RulePresetDto format (attribute-form rules).</summary>
    public static List<BasePriorityRule>? V1Rules(List<PriorityRuleDto>? dtos) =>
        dtos?.Select(r => r.ToRule()).Where(r => r != null).ToList();

    // The version-1 root elements the repository's dispatch matches.
    public const string PresetRoot = "SlaughterPreset";
    public const string RuleRoot = "RulePresetDto";

    /// <summary>The rules of a RulePresetDto-rooted file; null when the text is not one.</summary>
    public static List<PriorityRuleDto>? ReadRulePreset(string? text)
    {
        var dto = PresetSerializer.Deserialize<RulePresetDto>(text);
        return dto?.rules?.ToList();
    }

    /// <summary>The kind slices of a version-1 or older preset: the v1 carrier's own, or the
    /// old format's converted; null when the text is not a preset.</summary>
    private static List<KindPreset>? ReadV1Kinds(string? text)
    {
        var dto = PresetSerializer.Deserialize<PresetDto>(text);

        if (dto?.Kinds.Any(k => k.PrioRulesAdultMale?.Length > 0 || k.PrioRulesYoungMale?.Length > 0
            || k.PrioRulesAdultFemale?.Length > 0 || k.PrioRulesYoungFemale?.Length > 0) == true)
        {
            return [.. dto.Kinds.Select(V1ToCurrent).Where(k => k != null)];
        }

        // An old format has no rule lists — its kinds come converted; a current prefs-only
        // v1 preset stays as parsed.
        return TryReadPreset(text) ?? (dto == null ? null : [.. dto.Kinds.Select(V1ToCurrent).Where(k => k != null)]);
    }

    private static KindPreset? V1ToCurrent(KindPresetDto v1) => KindPresetFile.FromV1(v1)?.Kind;

    /// <summary>The version-1 preset as the current All model; null when the text is not a
    /// preset.</summary>
    public static AllKindsPreset? TryReadAll(string? text) =>
        ReadV1Kinds(text) is { } kinds ? new AllKindsPreset { Kinds = kinds } : null;

    /// <summary>The version-1 preset's kind slice: from an All-scope file the kind matching
    /// the target, otherwise the single kind; null when nothing applies.</summary>
    public static KindPresetFile? TryReadKind(PresetEntry entry, string? text, ThingDef targetDef)
    {
        var kinds = ReadV1Kinds(text);

        if (kinds == null || kinds.Count == 0)
        {
            return null;
        }

        if (entry.scope == PresetScope.All)
        {
            // Find the slice for the target kind; if absent, nothing applies.
            var match = kinds.FirstOrDefault(k => k.AnimalDefName == targetDef.defName);

            return match == null ? null : new KindPresetFile { Kind = match };
        }

        // Kind- and List-scope presets store a single kind whose defName is the SOURCE kind —
        // on apply we ignore it and use the list/settings for the TARGET kind.
        return new KindPresetFile { Kind = kinds[0] };
    }

    /// <summary>A rule list of a legacy file: the RulePresetDto format, the v1 unified
    /// carrier's kind slice, or the condition-preset format; null when nothing readable.</summary>
    public static ListPresetFile? TryReadList(PresetEntry entry, string? text)
    {
        switch (RootElementOf(text))
        {
            case RuleRoot:
                return FromRules(V1Rules(ReadRulePreset(text)));
            case PresetRoot:
                // The v1 unified carrier: the first kind's slice.
                var kind = ReadV1Kinds(text)?.FirstOrDefault();

                return kind == null ? null : ListSliceOf(new KindPresetFile { Kind = kind });
            default:
                return FromRules(TryReadConditions(entry));
        }
    }

    private static ListPresetFile? FromRules(List<BasePriorityRule>? rules) =>
        rules == null ? null : new ListPresetFile { Rules = rules.Select(r => r.ToPreset()).ToList() };

    // A copy of the repository's kind-slice reader — Legacy stays self-contained and goes
    // away together with this folder.
    private static ListPresetFile? ListSliceOf(KindPresetFile file)
    {
        var typed = new[] { file.Kind.Priorities.PrioRulesAdultMale, file.Kind.Priorities.PrioRulesYoungMale, file.Kind.Priorities.PrioRulesAdultFemale, file.Kind.Priorities.PrioRulesYoungFemale }
            .FirstOrDefault(rules => rules.Count > 0);

        return typed != null ? new ListPresetFile { Rules = typed }
            : file.Kind.SpecialSettings.Count > 0 ? ListPresetFile.OfSections(KindPresetFileSections.SectionsOf(file)) : null;
    }

    /// <summary>Root element name of the file — identifies which of the historical formats it is.</summary>
    private static string? RootElementOf(string? text)
    {
        if (text == null)
        {
            return null;
        }

        try
        {
            using (var reader = XmlReader.Create(new StringReader(text)))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        return reader.LocalName;
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static List<ConditionDto>? ReadConditionPreset(string? text)
    {
        if (text == null)
        {
            return null;
        }

        try
        {
            using var reader = new StringReader(text);
            var dto = (ConditionPresetDto)PresetSerializer.Serializer<ConditionPresetDto>().Deserialize(reader);

            return dto?.conditions?.ToList();
        }
        catch
        {
            return null;
        }
    }
}
