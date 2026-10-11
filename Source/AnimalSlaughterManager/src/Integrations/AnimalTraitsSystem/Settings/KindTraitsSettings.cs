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
}

public abstract class KindTraitsRuleSet<T> : IEditableTraitsRuleSet<T> where T : ITraitRule
{
    public List<T> rules = [];

    public int Count => rules.Count;

    public bool HasRules => rules.Count > 0;

    public System.Collections.IList Rules => rules;

    public abstract ITraitRulesListSection<T> GetEditor();

    public abstract string PresetsFolder { get; }

    public abstract string ListElementName { get; }

    public abstract void WriteXml(XmlElement parent);

    public abstract bool ReadXml(XmlElement kind);

    public void Clear()
    {
        rules.Clear();
        SettingsChanges.Raise();
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

        SettingsChanges.Raise();
    }

    public T Get(int i) => rules[i];

    public void Add(List<HediffDef> traits)
    {
        rules.AddRange(traits.Select(t => (T)Activator.CreateInstance(typeof(T), t)));
        SettingsChanges.Raise();
    }

    public void CopyAt(int i)
    {
        rules.Insert(i + 1, (T)rules[i].Copy());
        SettingsChanges.Raise();
    }

    public void RemoveAt(int i)
    {
        rules.RemoveAt(i);
        SettingsChanges.Raise();
    }

    public void ReplaceAt(int i, List<HediffDef> traits)
    {
        if (traits.Count == 1)
        {
            rules[i].SetTrait(traits.First());
            SettingsChanges.Raise();

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
        SettingsChanges.Raise();
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
public class KindTraitsSettings : IKindIntegrationSettings
{
    public const string SettingsKey = "atsTraits";

    /// <summary>The storage provider: registered unconditionally, so the settings survive ATS
    /// being disabled.</summary>
    public sealed class Provider : IKindIntegrationSettingsProvider
    {
        public static readonly Provider Instance = new();

        public string KindSettingsKey => SettingsKey;

        public IKindIntegrationSettings CreateKindSettings() => new KindTraitsSettings();
    }

    /// <summary>The kind's ATS settings, created on first use.</summary>
    public static KindTraitsSettings Of(KindSettings ks) => ks.GetOrCreateIntegration<KindTraitsSettings>(Provider.Instance);

    // Breeding: protect these animals from slaughter.
    public ProtectRuleSet protectRuleSet = new();

    // Force slaughter: cull these animals regardless of count/limits and other settings
    // (protection still wins at intersections).
    public ForceCullRuleSet forceCullRuleSet = new();

    /// <summary>True when this kind deviates from vanilla behaviour and must be recomputed.</summary>
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

    public void WriteXml(XmlElement parent)
    {
        var section = new TraitsSectionDto
        {
            KeepTraits = protectRuleSet.rules.Select(TraitDto.From).ToList(),
            ForceCullTraits = forceCullRuleSet.rules.Select(TraitDto.From).ToList(),
        };

        XmlSections.Serialize(parent, section);
    }

    public void ReadXml(XmlElement kind)
    {
        protectRuleSet.rules.Clear();
        forceCullRuleSet.rules.Clear();

        var section = kind[SettingsKey];

        if (section != null)
        {
            var dto = XmlSections.Deserialize<TraitsSectionDto>(section);

            if (dto != null)
            {
                FillRules(dto.KeepTraits, dto.ForceCullTraits);
            }

            return;
        }

        // Presets saved before the section format carried the lists as direct children.
        var legacyKeep = kind[nameof(TraitsSectionDto.KeepTraits)];

        if (legacyKeep != null)
        {
            FillRules(XmlSections.Deserialize<KeepTraitsListDto>(legacyKeep)?.Trait, null);
        }

        var legacyCull = kind[nameof(TraitsSectionDto.ForceCullTraits)];

        if (legacyCull != null)
        {
            FillRules(null, XmlSections.Deserialize<ForceCullTraitsListDto>(legacyCull)?.Trait);
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

    public override string ListElementName => nameof(TraitsSectionDto.KeepTraits);

    public override void WriteXml(XmlElement parent)
    {
        XmlSections.Serialize(parent, new KeepTraitsListDto { Trait = rules.Select(TraitDto.From).ToList() });
    }

    public override bool ReadXml(XmlElement kind)
    {
        var list = kind[ListElementName];

        if (list == null)
        {
            return false;
        }

        rules.Clear();

        foreach (var t in (XmlSections.Deserialize<KeepTraitsListDto>(list)?.Trait ?? []).Select(t => t.ToTarget()))
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
        XmlSections.Serialize(parent, new ForceCullTraitsListDto { Trait = rules.Select(TraitDto.From).ToList() });
    }

    public override bool ReadXml(XmlElement kind)
    {
        var list = kind[ListElementName];

        if (list == null)
        {
            return false;
        }

        rules.Clear();

        foreach (var c in (XmlSections.Deserialize<ForceCullTraitsListDto>(list)?.Trait ?? []).Select(t => t.ToCull()))
        {
            if (c != null)
            {
                rules.Add(c);
            }
        }

        return true;
    }
}
