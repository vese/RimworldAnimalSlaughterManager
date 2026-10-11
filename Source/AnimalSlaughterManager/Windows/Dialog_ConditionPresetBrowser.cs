using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// Preset browser for one condition-list bucket (age×sex). Lists the condition presets of all
/// four buckets plus Kind/All presets (their bucket slice is taken on apply); cross-bucket rows
/// carry the bucket's mark. Saving writes the current bucket's list. The frame lives in
/// <see cref="Dialog_PresetBrowserBase"/>.
/// </summary>
public class Dialog_ConditionPresetBrowser : Dialog_PresetBrowserBase
{
    private readonly KindPrioritySettings prioritySettings;
    private readonly bool male;
    private readonly bool adult;

    public Dialog_ConditionPresetBrowser(ASM_MapComp comp, bool male, bool adult, KindPrioritySettings prioritySettings)
    {
        this.prioritySettings = prioritySettings;
        this.male = male;
        this.adult = adult;
        optionalTitle = ASMKeys.CondPresetTitle.Translate(BucketLabel(male, adult));
    }

    private static string BucketLabel(bool male, bool adult)
    {
        switch (male, adult)
        {
            case (true, true):
                return ASMKeys.AdultMales.Translate();
            case (true, false):
                return ASMKeys.YoungMales.Translate();
            case (false, true):
                return ASMKeys.AdultFemales.Translate();
            default:
                return ASMKeys.YoungFemales.Translate();
        }
    }

    // ---- Policy ---------------------------------------------------------------------------

    protected override IEnumerable<PresetEntry> ListEntries() => PresetRepository.ListConditionPresets();

    protected override bool HasData() => prioritySettings.Get(male, adult).Count > 0;

    protected override void Save(string name) =>
        PresetRepository.ExportConditions(name, (male, adult), prioritySettings.Get(male, adult));

    protected override bool Apply(PresetEntry entry)
    {
        // A same-kind condition preset carries its rule list; a Kind/All preset — the slice
        // of this bucket.
        var rules = entry.scope == PresetScope.List && entry.condBucket != null
            ? PresetRepository.ApplyConditions(entry)
            : PresetRepository.ReadKindRules(entry, (male, adult));

        if (rules == null || rules.Count == 0)
        {
            return false;
        }

        prioritySettings.ReplaceAll(male, adult, rules);
        return true;
    }

    protected override string NoSliceLabel() => BucketLabel(male, adult);

    // Kind/All rows carry their scope mark; a condition preset saved in another bucket —
    // that bucket's label.
    protected override string? MarkOf(PresetEntry entry)
    {
        if (entry.scope == PresetScope.All)
        {
            return ASMKeys.PresetAllMark.Translate();
        }

        if (entry.scope == PresetScope.Kind)
        {
            return ASMKeys.PresetKindMark.Translate();
        }

        if (entry.scope == PresetScope.List && entry.condBucket is { } b && b != (male, adult))
        {
            return BucketLabel(b.Male, b.Adult);
        }

        return null;
    }

    protected override string? RowTip(PresetEntry entry) =>
        entry.scope == PresetScope.Kind || entry.scope == PresetScope.All
            ? ASMKeys.CondPresetCrossBucket.Translate(entry.scope.ToString())
            : null;

    protected override bool CanOverwrite(PresetEntry entry) =>
        entry.scope == PresetScope.List && entry.condBucket == (male, adult) && HasData();
}
