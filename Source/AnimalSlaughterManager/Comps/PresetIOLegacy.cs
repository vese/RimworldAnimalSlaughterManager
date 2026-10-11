using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ASM;

/// <summary>
/// Reading presets saved by older mod versions, kept apart from the current formats:
/// root-folder <c>*.xml</c> files (saved before scopes existed — they are All-animals presets)
/// and condition lists stored as <see cref="SlaughterCondition"/> (before the priority-rule
/// refactor). Remove this class together with <see cref="KindPrioritySettingsLegacy"/> when the
/// save-compatibility horizon allows.
/// </summary>
public static class PresetIOLegacy
{
    /// <summary>Adds root-folder presets to an All-scope listing (they have no scope folder).</summary>
    public static void AddRootPresets(List<PresetEntry> result)
    {
        if (!Directory.Exists(PresetIO.RootFolder))
        {
            return;
        }

        foreach (var p in Directory.GetFiles(PresetIO.RootFolder, "*.xml", SearchOption.TopDirectoryOnly))
        {
            result.Add(new PresetEntry
            {
                name = Path.GetFileNameWithoutExtension(p),
                path = p,
                date = File.GetLastWriteTime(p),
                scope = PresetScope.All
            });
        }
    }

    /// <summary>Condition rules of a legacy <see cref="ConditionPresetDto"/> file; null when the
    /// file is not one (the current-format reader falls back here).</summary>
    public static List<BasePriorityRule>? TryReadConditions(PresetEntry entry)
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
}
