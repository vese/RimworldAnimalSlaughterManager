#pragma warning disable CS0612, CS0618

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace ASM;

/// <summary>
/// The legacy ATS storage of the keep/force-cull lists: loads the old "atsTraits" data — the
/// save entries and the preset sections (including the pre-section flat children) — and
/// migrates the rules into the core's KindSpecialRules, which owns the lists now. Delete
/// together with the rest of the legacy data when the save-compatibility horizon allows.
/// </summary>
public class KindTraitsSettings : IKindSettings, IOwnedKindSettings
{
    public const string SettingsKey = "atsTraits";

    /// <summary>The storage provider: registered unconditionally, so the old data survives ATS
    /// being disabled and still migrates.</summary>
    public sealed class Provider : IKindSettingsProvider
    {
        public static readonly Provider Instance = new();

        public string Key => SettingsKey;

        public IKindSettings Create(KindSettings owner) => new KindTraitsSettings(owner);
    }

    private KindSettings? owner;

    // The old save containers, loaded by Scribe and migrated on PostLoadInit.
    private List<TraitProtectRule> keepTraits = [];
    private List<TraitRule> forceCullTraits = [];

    public KindTraitsSettings() { }

    public KindTraitsSettings(KindSettings owner)
    {
        this.owner = owner;
    }

    public void Attach(KindSettings owner) => this.owner = owner;

    // The core storage carries the state now.
    public bool Customized => false;

    public void Reset() { }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref keepTraits, "keepTraits", LookMode.Deep);
        Scribe_Collections.Look(ref forceCullTraits, "forceCullTraits", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit && (keepTraits?.Count > 0 || forceCullTraits?.Count > 0))
        {
            MigrateRules();
        }
    }

    // New presets carry the core's own section.
    public void WriteXml(XmlElement parent) { }

    public void ReadXml(XmlElement kind)
    {
        if (owner == null)
        {
            return;
        }

        var section = kind[SettingsKey];

        if (section != null)
        {
            var dto = PresetSectionSerialization.Deserialize<TraitsSectionDto>(section);

            if (dto != null)
            {
                FillCore(dto.KeepTraits, dto.ForceCullTraits);
            }

            return;
        }

        // Presets saved before the section format carried the lists as direct children.
        var legacyKeep = kind[nameof(TraitsSectionDto.KeepTraits)];

        if (legacyKeep != null)
        {
            FillCore(PresetSectionSerialization.Deserialize<KeepTraitsListDto>(legacyKeep)?.Trait, null);
        }

        var legacyCull = kind[nameof(TraitsSectionDto.ForceCullTraits)];

        if (legacyCull != null)
        {
            FillCore(null, PresetSectionSerialization.Deserialize<ForceCullTraitsListDto>(legacyCull)?.Trait);
        }
    }

    private void MigrateRules()
    {
        if (owner == null)
        {
            return;
        }

        owner.specialRules.FillKeep(Convert([.. keepTraits.Where(r => r?.trait != null).Select(TraitDto.From)]));
        owner.specialRules.FillCull(Convert([.. forceCullTraits.Where(r => r?.trait != null).Select(TraitDto.From)]));

        keepTraits.Clear();
        forceCullTraits.Clear();

        // The legacy entry has nothing left to hold.
        owner.integrationsSettings.Remove(SettingsKey);
    }

    private static List<SpecialRuleDto> Convert(List<TraitDto> list) =>
        [.. list.Select(t => new SpecialRuleDto { target = t.trait, keepCount = t.keepCount, ageScope = t.ageScope, genderScope = t.genderScope, inheritMode = t.inheritMode })];

    private void FillCore(List<TraitDto>? keep, List<TraitDto>? cull)
    {
        if (keep != null)
        {
            owner!.specialRules.FillKeep(Convert(keep));
        }

        if (cull != null)
        {
            owner!.specialRules.FillCull(Convert(cull));
        }
    }
}
