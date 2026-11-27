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
    private readonly string bucket;

    protected override IEnumerable<PresetScope> OfferedScopes => [PresetScope.PriorityList, PresetScope.Kind, PresetScope.All];

    protected override bool HasData => prioritySettings.Get(male, adult).Count > 0;

    public KindPrioritiesListPresetBrowserDialog(bool male, bool adult, KindPrioritySettings prioritySettings)
    {
        this.male = male;
        this.adult = adult;
        this.prioritySettings = prioritySettings;
        bucket = PriorityPresetBuckets.GetBucket((male, adult));
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

    protected override void Save(string name) => PresetRepository.Save(
        name,
        PresetScope.PriorityList,
        PriorityListPresetFile.OfRules(prioritySettings.ToPreset((male, adult))),
        bucket);

    protected override bool Apply(PresetEntry entry)
    {
        // A condition preset carries its rule list; a Kind/All preset — the slice of this
        // bucket (cross-bucket import included).
        var dto = PresetRepository.ReadPriorityList(entry);

        if (dto == null)
        {
            return false;
        }

        prioritySettings.FromPreset(dto.Rules, (male, adult));
        return true;
    }

    // Kind/All rows carry their scope mark; a condition preset saved in another bucket —
    // that bucket's label.
    protected override string? GetRowMark(PresetEntry entry)
    {
        if (entry.scope == PresetScope.All)
        {
            return ASMKeys.PresetAllMark.Translate();
        }

        if (entry.scope == PresetScope.Kind)
        {
            return ASMKeys.PresetKindMark.Translate();
        }

        if (entry.scope == PresetScope.PriorityList && entry.bucket != bucket && PriorityPresetBuckets.GetKey(entry.bucket) is { } b)
        {
            return GetBucketLabel(b.Male, b.Adult);
        }

        return null;
    }

    protected override string? GetRowTip(PresetEntry entry) =>
        entry.scope == PresetScope.Kind || entry.scope == PresetScope.All
            ? ASMKeys.CondPresetCrossBucket.Translate(entry.scope.ToString())
            : null;

    protected override bool CanOverwrite(PresetEntry entry) =>
        entry.scope == PresetScope.PriorityList && entry.bucket == bucket && HasData;
}
