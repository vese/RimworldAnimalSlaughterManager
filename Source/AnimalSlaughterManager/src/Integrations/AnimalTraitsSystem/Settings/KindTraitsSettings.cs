using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public abstract void Save(KindDto dto);

    public abstract bool Load(KindDto dto);

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

public class KindTraitsSettings : IPresettable
{
    // Breeding: protect these animals from slaughter.
    public ProtectRuleSet protectRuleSet = new();
    // Force slaughter: cull these animals regardless of count/limits and other settings
    // (protection still wins at intersections).
    public ForceCullRuleSet forceCullRuleSet = new();
    /// <summary>True when this kind deviates from vanilla behaviour and must be recomputed.</summary>
    public bool Customized =>
        protectRuleSet.HasRules ||
        forceCullRuleSet.HasRules;    ///// <summary>Priority customization: sex×age (older/younger) prefs, condition lists, or cull/spare trait lists.</summary>
    //public bool HasPrioritySettings =>
    //    cullTraits != null && cullTraits.Count > 0 ||
    //    spareTraits != null && spareTraits.Count > 0;

    /// <summary>Protection customization: breeding ("keep") trait targets (protect from slaughter).</summary>
    public bool HasProtectionSettings => protectRuleSet.HasRules;

    /// <summary>Force-slaughter customization: cull matching animals regardless of count/limits.</summary>
    public bool HasForceCullSettings => forceCullRuleSet.HasRules;

    public void Reset()
    {
        protectRuleSet.Clear();
        forceCullRuleSet.Clear();
    }

    public void ExposeData()
    {
        protectRuleSet.ExposeData("keepTraits");
        forceCullRuleSet.ExposeData("forceCullTraits");

        //Scribe_Collections.Look(ref keepTraits.rules, "keepTraits", LookMode.Deep);
        //Scribe_Collections.Look(ref forceCullTraits, "forceCullTraits", LookMode.Deep);
        //Scribe_Collections.Look(ref cullTraits, "cullTraits", LookMode.Deep);
        //Scribe_Collections.Look(ref spareTraits, "spareTraits", LookMode.Deep);

        //if (Scribe.mode == LoadSaveMode.PostLoadInit)
        //{
        //    keepTraits ??= [];
        //    forceCullTraits ??= [];
        //    cullTraits ??= [];
        //    spareTraits ??= [];

        //    // A trait/disease/trainable def may resolve to null when the mod that defined it
        //    // (e.g. Animal Traits System) was disabled on this save. Drop those dead entries so
        //    // the settings don't fill up with no-op "?" rows. (This does not silence RimWorld's
        //    // own "Could not load reference" log for hediffs still on the pawns — that is the
        //    // base game resolving the save, outside this mod's control.)
        //    keepTraits.RemoveAll(t => t == null || t.trait == null);
        //    forceCullTraits.RemoveAll(t => t == null || t.trait == null);
        //    cullTraits.RemoveAll(t => t == null || t.trait == null);
        //    spareTraits.RemoveAll(t => t == null || t.trait == null);
        //}
    }

    public void Save(KindDto dto)
    {
        dto.KeepTraits = protectRuleSet.rules.Select(TraitDto.From).ToList();
        dto.ForceCullTraits = forceCullRuleSet.rules.Select(TraitDto.From).ToList();
    }

    public void Load(KindDto dto)
    {
        protectRuleSet.rules.Clear();
        forceCullRuleSet.rules.Clear();

        if (dto.KeepTraits != null)
        {
            foreach (var t in dto.KeepTraits.Select(t => t.ToTarget()))
            {
                if (t != null)
                {
                    protectRuleSet.rules.Add(t);
                }
            }
        }

        if (dto.ForceCullTraits != null)
        {
            foreach (var c in dto.ForceCullTraits.Select(t => t.ToCull()))
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

    public override void Save(KindDto dto) =>
        dto.KeepTraits = rules.Select(TraitDto.From).ToList();

    public override bool Load(KindDto dto)
    {
        if (dto.KeepTraits == null)
        {
            return false;
        }

        rules.Clear();

        foreach (var t in dto.KeepTraits.Select(t => t.ToTarget()))
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

    public override void Save(KindDto dto) =>
        dto.ForceCullTraits = rules.Select(TraitDto.From).ToList();

    public override bool Load(KindDto dto)
    {
        if (dto.ForceCullTraits == null)
        {
            return false;
        }

        rules.Clear();

        foreach (var c in dto.ForceCullTraits.Select(t => t.ToCull()))
        {
            if (c != null)
            {
                rules.Add(c);
            }
        }

        return true;
    }
}
