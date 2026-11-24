using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace ASM;

/// <summary>An editable list of special rules: the shared section UI edits it in place, and
/// list presets ride the preset carrier through the IPresettableRuleSet slot.</summary>
public abstract class SpecialRuleSet<T> : IPresettableRuleSet where T : SpecialRule, new()
{
    public List<T> rules = [];

    public int Count => rules.Count;

    public bool HasRules => rules.Count > 0;

    public IList Rules => rules;

    public T Get(int i) => rules[i];

    public abstract string PresetsFolder { get; }

    public abstract string ListElementName { get; }

    public abstract void WriteXml(System.Xml.XmlElement parent);

    public abstract bool ReadXml(System.Xml.XmlElement kind);

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

    public void Add(List<Def> targets)
    {
        foreach (var target in targets)
        {
            var rule = new T();
            rule.SetTarget(target);
            rules.Add(rule);
        }

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

    // One picked target replaces the rule's own; several split it into a row per target.
    public void ReplaceAt(int i, List<Def> targets)
    {
        if (targets.Count == 1)
        {
            rules[i].SetTarget(targets[0]);
            SettingsChangeTracker.Raise();

            return;
        }

        var oldRule = rules[i];
        var newRules = targets.Select(t =>
        {
            var newRule = (T)oldRule.Copy();
            newRule.SetTarget(t);
            return newRule;
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
            rules.RemoveAll(r => r is null || r.HasNullDef);
        }
    }

    public override string ToString() => $"{typeof(T).Name} x{rules.Count}";
}
