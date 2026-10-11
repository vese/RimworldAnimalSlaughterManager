using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ASM;

/// <summary>
/// Reading presets saved by older mod versions, kept apart from the current formats:
/// root-folder <c>*.xml</c> files (saved before scopes existed — they are All-animals presets)
/// and condition lists stored as <see cref="SlaughterCondition"/> (before the priority-rule
/// refactor). Remove this class together with <see cref="KindPrioritySettingsLegacy"/> when the
/// save-compatibility horizon allows.
/// </summary>
public static class PresetLegacyReader
{
    /// <summary>Adds root-folder presets to an All-scope listing (they have no scope folder).</summary>
    public static void AddRootPresets(List<PresetEntry> result)
    {
        foreach (var p in PresetRepository.Store.Files(PresetRepository.RootFolder))
        {
            result.Add(new PresetEntry
            {
                name = Path.GetFileNameWithoutExtension(p),
                path = p,
                date = PresetRepository.Store.LastWrite(p),
                scope = PresetScope.All
            });
        }
    }

    /// <summary>Condition rules of a legacy <see cref="ConditionPresetDto"/> file; null when the
    /// file is not one (the current-format reader falls back here).</summary>
    public static List<BasePriorityRule>? TryReadConditions(PresetEntry entry)
    {
#pragma warning disable CS0612, CS0618
        var conditions = PresetFormats.ReadConditionPreset(PresetRepository.Store.ReadText(entry.path));

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
#pragma warning restore CS0612, CS0618
    }
}
