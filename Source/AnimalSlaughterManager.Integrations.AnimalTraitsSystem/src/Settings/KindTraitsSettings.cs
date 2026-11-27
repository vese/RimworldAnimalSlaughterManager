using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace ASM;

public interface IEditableTraitsRuleSet<T> : IPresettableRuleSet where T : ITraitRule
{
    int Count { get; }
    bool HasRules { get; }
    System.Collections.IList Rules { get; }
    void Clear();
    void Swap(int from, int to);
    T Get(int i);
    void Add(List<HediffDef> traits);
    void CopyAt(int i);
    void RemoveAt(int i);
    void ReplaceAt(int i, List<HediffDef> items);

    ListPresetFile ToPreset();

    bool FromPreset(ListPresetFile dto);
}

public abstract class KindTraitsRuleSet<T> : IEditableTraitsRuleSet<T> where T : ITraitRule
{
    public List<T> rules = [];

    public int Count => rules.Count;

    public bool HasRules => rules.Count > 0;

    public System.Collections.IList Rules => rules;

    public abstract ITraitRulesListSection<T> GetEditor();

    /// <summary>The rule set as a list preset: the section element is the payload.</summary>
    public ListPresetFile ToPreset()
    {
        var holder = PresetSectionSerialization.Holder();
        WriteXml(holder);
        return ListPresetFile.OfSections(PresetSectionSerialization.Children(holder));
    }

    public bool FromPreset(ListPresetFile dto)
    {
        var kind = PresetSectionSerialization.Wrap([.. dto.Sections]);

        if (!ReadXml(kind))
        {
            return false;
        }

        SettingsChangeTracker.Raise();
        return true;
    }


    public abstract string PresetsFolder { get; }

    public abstract string ListElementName { get; }

    public abstract void WriteXml(XmlElement parent);

    public abstract bool ReadXml(XmlElement kind);

    public void Clear()
    {
        rules.Clear();
        SettingsChangeTracker.Raise();
    }

    public void Swap(int from, int to)
    {
        if (from < 0 || from >= rules.Count || to < 0 || to > rules.Count || from == to)
        {
            return;
        }

        var item = rules[from];

        rules.RemoveAt(from);

        if (from < to)
        {
            rules.Insert(to - 1, item);
        }
        else
        {
            rules.Insert(to, item);
        }

        SettingsChangeTracker.Raise();
    }

    public T Get(int i) => rules[i];

    public void Add(List<HediffDef> traits)
    {
        rules.AddRange(traits.Select(t => (T)Activator.CreateInstance(typeof(T), t)));
        SettingsChangeTracker.Raise();
    }

    public void CopyAt(int i)
    {
        rules.Insert(i + 1, (T)rules[i].Copy());
        SettingsChangeTracker.Raise();
    }

    public void RemoveAt(int i)
    {
        rules.RemoveAt(i);
        SettingsChangeTracker.Raise();
    }

    public void ReplaceAt(int i, List<HediffDef> traits)
    {
        if (traits.Count == 1)
        {
            rules[i].SetTrait(traits.First());
            SettingsChangeTracker.Raise();

            return;
        }

        var oldRules = rules[i];
        var newRules = traits.Select(t =>
        {
            var newRule = oldRules.Copy();
            newRule.SetTrait(t);
            return (T)newRule;
        });

        rules.RemoveAt(i);
        rules.InsertRange(i, newRules);
        SettingsChangeTracker.Raise();
    }

    public void ExposeData(string label)
    {
        Scribe_Collections.Look(ref rules, label, LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            rules ??= [];
            rules.RemoveAll(t => t is null || t.HasNullDef);
        }
    }
}

/// <summary>
/// The ATS-owned per-kind settings: the keep (breeding protection) and force-cull trait lists.
/// Stored opaquely by the core under <see cref="SettingsKey"/>; in kind presets they ride as the
/// integration's own section (the legacy flat children of earlier versions also read).
/// </summary>
public class TraitsPreset : BaseSpecialSettingsPreset
{
    public List<TraitDto> KeepTraits = [];
    public List<TraitDto> ForceCullTraits = [];
}

public class KindTraitsSettings : ISpecialSettings
{
    public const string SettingsKey = "atsTraits";

    /// <summary>The storage provider: registered unconditionally, so the settings survive ATS
    /// being disabled. Owns the model's behavior: queries, reset, save scribing, the preset
    /// XML section.</summary>
    public sealed class Provider : IKindSettingsProvider
    {
        public static readonly Provider Instance = new();

        public string Key => SettingsKey;

        public ISpecialSettings Create() => new KindTraitsSettings();

        public void WriteSection(BaseSpecialSettingsPreset preset, XmlElement parent)
        {
            if (preset is not TraitsPreset p)
            {
                return;
            }

            PresetSectionSerialization.Serialize(parent, new TraitsSectionDto
            {
                KeepTraits = p.KeepTraits,
                ForceCullTraits = p.ForceCullTraits
            });
        }

        public BaseSpecialSettingsPreset? ReadSection(XmlElement kind)
        {
            var section = kind[SettingsKey];

            if (section != null)
            {
                return new TraitsPreset
                {
                    KeepTraits = PresetSectionSerialization.Deserialize<TraitsSectionDto>(section)?.KeepTraits ?? [],
                    ForceCullTraits = PresetSectionSerialization.Deserialize<TraitsSectionDto>(section)?.ForceCullTraits ?? []
                };
            }

            // Presets saved before the section format carried the lists as direct children.
            var legacyKeep = kind[nameof(TraitsSectionDto.KeepTraits)];
            var legacyCull = kind[nameof(TraitsSectionDto.ForceCullTraits)];

            if (legacyKeep == null && legacyCull == null)
            {
                return null;
            }

            return new TraitsPreset
            {
                KeepTraits = PresetSectionSerialization.Deserialize<KeepTraitsListDto>(legacyKeep)?.Trait ?? [],
                ForceCullTraits = PresetSectionSerialization.Deserialize<ForceCullTraitsListDto>(legacyCull)?.Trait ?? []
            };
        }


    }

    /// <summary>The kind's ATS settings, created on first use.</summary>
    public static KindTraitsSettings Of(KindSettings ks) => ks.GetOrCreateIntegration<KindTraitsSettings>(Provider.Instance);

    // Breeding: protect these animals from slaughter.
    public ProtectRuleSet protectRuleSet = new();

    // Force slaughter: cull these animals regardless of count/limits and other settings
    // (protection still wins at intersections).
    public ForceCullRuleSet forceCullRuleSet = new();

    public bool Customized =>
        protectRuleSet.HasRules ||
        forceCullRuleSet.HasRules;

    public void Reset()
    {
        protectRuleSet.Clear();
        forceCullRuleSet.Clear();
    }

    public void ExposeData()
    {
        protectRuleSet.ExposeData("keepTraits");
        forceCullRuleSet.ExposeData("forceCullTraits");
    }

    public BaseSpecialSettingsPreset ToPreset() => new TraitsPreset()
    {
        KeepTraits = protectRuleSet.rules.Select(TraitDto.From).ToList(),
        ForceCullTraits = forceCullRuleSet.rules.Select(TraitDto.From).ToList()
    };

    public void FromPreset(BaseSpecialSettingsPreset preset)
    {
        protectRuleSet.rules.Clear();
        forceCullRuleSet.rules.Clear();

        if (preset is TraitsPreset p)
        {
            FillRules(p.KeepTraits, p.ForceCullTraits);
        }
    }

    private void FillRules(List<TraitDto>? keep, List<TraitDto>? cull)
    {
        if (keep != null)
        {
            foreach (var t in keep.Select(t => t.ToTarget()))
            {
                if (t != null)
                {
                    protectRuleSet.rules.Add(t);
                    protectRuleSet.rules.Add(t);
                }
            }
        }

        if (cull != null)
        {
            foreach (var c in cull.Select(t => t.ToCull()))
            {
                if (c != null)
                {
                    forceCullRuleSet.rules.Add(c);
                }
            }
        }
    }
}

public class ProtectRuleSet() : KindTraitsRuleSet<TraitProtectRule>
{
    public override ITraitRulesListSection<TraitProtectRule> GetEditor() => new TraitProtectRulesListSection(this);

    public override string PresetsFolder => "Keep";

    public override string ListElementName => nameof(TraitsSectionDto.KeepTraits);

    public override void WriteXml(XmlElement parent)
    {
        PresetSectionSerialization.Serialize(parent, new KeepTraitsListDto { Trait = rules.Select(TraitDto.From).ToList() });
    }

    public override bool ReadXml(XmlElement kind)
    {
        var list = kind[ListElementName];

        if (list == null)
        {
            return false;
        }

        rules.Clear();

        foreach (var t in (PresetSectionSerialization.Deserialize<KeepTraitsListDto>(list)?.Trait ?? []).Select(t => t.ToTarget()))
        {
            if (t != null)
            {
                rules.Add(t);
            }
        }

        return true;
    }
}

public class ForceCullRuleSet() : KindTraitsRuleSet<TraitRule>
{
    public override ITraitRulesListSection<TraitRule> GetEditor() => new TraitRulesListSection(this);

    public override string PresetsFolder => "ForceCull";

    public override string ListElementName => nameof(TraitsSectionDto.ForceCullTraits);

    public override void WriteXml(XmlElement parent)
    {
        PresetSectionSerialization.Serialize(parent, new ForceCullTraitsListDto { Trait = rules.Select(TraitDto.From).ToList() });
    }

    public override bool ReadXml(XmlElement kind)
    {
        var list = kind[ListElementName];

        if (list == null)
        {
            return false;
        }

        rules.Clear();

        foreach (var c in (PresetSectionSerialization.Deserialize<ForceCullTraitsListDto>(list)?.Trait ?? []).Select(t => t.ToCull()))
        {
            if (c != null)
            {
                rules.Add(c);
            }
        }

        return true;
    }
}
