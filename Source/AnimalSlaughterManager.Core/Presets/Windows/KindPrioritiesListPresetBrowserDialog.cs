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

    protected override (PresetScope Scope, IEnumerable<PresetEntry> Entries)[] ScopeSources =>
    [
        (PresetScope.List, PresetRepository.ListConditionPresets()),
        (PresetScope.Kind, PresetRepository.ListPresets(PresetScope.Kind)),
        (PresetScope.All, PresetRepository.ListPresets(PresetScope.All))
    ];

    protected internal override bool HasData => prioritySettings.Get(male, adult).Count > 0;

    public KindPrioritiesListPresetBrowserDialog(bool male, bool adult, KindPrioritySettings prioritySettings)
    {
        this.male = male;
        this.adult = adult;
        this.prioritySettings = prioritySettings;
        targetLabel = GetBucketLabel(male, adult);
        optionalTitle = ASMKeys.CondPresetTitle.Translate(targetLabel);
    }

    private static TaggedString GetBucketLabel(bool male, bool adult) => (male, adult) switch
    {
        (true, true) => ASMKeys.AdultMales.Translate(),
        (true, false) => ASMKeys.YoungMales.Translate(),
        (false, true) => ASMKeys.AdultFemales.Translate(),
        _ => ASMKeys.YoungFemales.Translate(),
    };

    protected internal override void Save(string name) =>
        PresetRepository.SaveList(name, PresetRepository.ConditionsFolder((male, adult)), ListPresetFile.OfRules(prioritySettings.ToPreset((male, adult))));

    protected internal override bool Apply(PresetEntry entry)
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
    protected internal override string? GetRowMark(PresetEntry entry)
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
            return GetBucketLabel(b.Male, b.Adult);
        }

        return null;
    }

    protected internal override string? GetRowTip(PresetEntry entry) =>
        entry.scope == PresetScope.Kind || entry.scope == PresetScope.All
            ? ASMKeys.CondPresetCrossBucket.Translate(entry.scope.ToString())
            : null;

    protected internal override bool CanOverwrite(PresetEntry entry) =>
        entry.scope == PresetScope.List && entry.condBucket == (male, adult) && HasData;
}
