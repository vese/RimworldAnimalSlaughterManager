#pragma warning disable CS0612, CS0618

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    /// <summary>The list payload of a version-1 kind/all/unified slice: the first non-empty
    /// rule bucket, or the raw integration sections (a special-settings list); both the current
    /// and the v1 kind forms.</summary>
    public static ListPresetFile? FromSliceV1(string? text, string root)
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

    /// <summary>The rules of the pre-unification RulePresetDto format (attribute-form rules).</summary>
    public static List<BasePriorityRule>? V1Rules(List<PriorityRuleDto>? dtos) =>
        dtos?.Select(r => r.ToRule()).Where(r => r != null).ToList();

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
