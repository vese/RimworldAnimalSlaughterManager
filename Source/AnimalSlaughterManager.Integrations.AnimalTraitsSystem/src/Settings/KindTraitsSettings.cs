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

    SpecialListPreset ToPreset();

    bool FromPreset(SpecialListPreset dto);
}

public abstract class KindTraitsRuleSet<T> : IEditableTraitsRuleSet<T> where T : ITraitRule
{
    public List<T> rules = [];

    public int Count => rules.Count;

    public bool HasRules => rules.Count > 0;

    public System.Collections.IList Rules => rules;

    public abstract ITraitRulesListSection<T> GetEditor();

    /// <summary>The rule set as a list preset: its own list of the provider's settings model
    /// (the other lists stay untouched — the per-list semantics).</summary>
    public SpecialListPreset ToPreset()
    {
        var traits = new TraitsPreset();
        WriteTraits(traits);

        return new SpecialListPreset { Settings = { [KindTraitsSettings.SettingsKey] = traits } };
    }

    public bool FromPreset(SpecialListPreset dto)
    {
        if (dto.Settings.TryGetValue(KindTraitsSettings.SettingsKey, out var settings) && settings is TraitsPreset traits)
        {
            ReadTraits(traits);
            SettingsChangeTracker.Raise();

            return true;
        }

        return false;
    }


    public abstract string PresetsFolder { get; }


    /// <summary>Fills the settings model with this rule set's own list.</summary>
    protected internal abstract void WriteTraits(TraitsPreset preset);

    /// <summary>Replaces this rule set's rules with its own list of the settings model.</summary>
    protected internal abstract void ReadTraits(TraitsPreset preset);

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

#pragma warning disable CS0612, CS0618
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
#pragma warning restore CS0612, CS0618

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

    protected internal override void WriteTraits(TraitsPreset preset)
    {
        preset.KeepTraits = rules.Select(TraitDto.From).ToList();
    }

    protected internal override void ReadTraits(TraitsPreset preset)
    {
        rules.Clear();

        foreach (var t in preset.KeepTraits.Select(t => t.ToTarget()))
        {
            if (t != null)
            {
                rules.Add(t);
            }
        }
    }
}

public class ForceCullRuleSet() : KindTraitsRuleSet<TraitRule>
{
    public override ITraitRulesListSection<TraitRule> GetEditor() => new TraitRulesListSection(this);

    public override string PresetsFolder => "ForceCull";

    protected internal override void WriteTraits(TraitsPreset preset)
    {
        preset.ForceCullTraits = rules.Select(TraitDto.From).ToList();
    }

    protected internal override void ReadTraits(TraitsPreset preset)
    {
        rules.Clear();

        foreach (var c in preset.ForceCullTraits.Select(t => t.ToCull()))
        {
            if (c != null)
            {
                rules.Add(c);
            }
        }
    }
}
