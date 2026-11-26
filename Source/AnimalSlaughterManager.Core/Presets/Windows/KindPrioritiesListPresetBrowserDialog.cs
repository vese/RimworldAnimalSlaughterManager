using System.Collections.Generic;

using Verse;

namespace ASM;

/// <summary>
/// Preset browser for one condition-list bucket (age×sex). Lists the condition presets of all
/// four buckets plus Kind/All presets (their bucket slice is taken on apply); cross-bucket rows
/// carry the bucket's mark. Saving writes the current bucket's list. The frame lives in
/// <see cref="BasePresetBrowserDialog"/>.
/// </summary>
public class KindPrioritiesListPresetBrowserDialog : BasePresetBrowserDialog
{
    private readonly KindPrioritySettings prioritySettings;
    private readonly bool male;
    private readonly bool adult;

    public KindPrioritiesListPresetBrowserDialog(ASM_MapComp comp, bool male, bool adult, KindPrioritySettings prioritySettings)
    {
        this.prioritySettings = prioritySettings;
        this.male = male;
        this.adult = adult;
        targetLabel = BucketLabel(male, adult);
        optionalTitle = ASMKeys.CondPresetTitle.Translate(targetLabel);
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

    protected override (PresetScope Scope, IEnumerable<PresetEntry> Entries)[] ScopeSources =>
    [
        (PresetScope.List, PresetRepository.ListConditionPresets()),
        (PresetScope.Kind, PresetRepository.ListPresets(PresetScope.Kind)),
        (PresetScope.All, PresetRepository.ListPresets(PresetScope.All))
    ];

    protected override bool HasData() => prioritySettings.Get(male, adult).Count > 0;

    protected override void Save(string name) =>
        PresetRepository.SaveList(name, PresetRepository.ConditionsFolder((male, adult)), ListPresetFile.OfRules(prioritySettings.ToPreset((male, adult))));

    protected override bool Apply(PresetEntry entry)
    {
        // A condition preset carries its rule list; a Kind/All preset — the slice of this
        // bucket (cross-bucket import included).
        var dto = PresetRepository.ReadList(entry);

        if (dto == null)
        {
            return false;
        }

        prioritySettings.FromPreset(dto.ToRuleList(), (male, adult));
        return true;
    }

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
