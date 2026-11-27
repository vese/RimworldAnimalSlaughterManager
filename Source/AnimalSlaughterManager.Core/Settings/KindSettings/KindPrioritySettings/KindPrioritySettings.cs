
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

public class KindPrioritySettings : IPresettable<PrioritySettingsPreset>
{
    private const int currentDataVersion = 1;

    // New instances start at the current version so they serialize in the current format;
    // loading an old save overwrites this with the saved version and migrates in PostLoadInit.
    private int dataVersion = currentDataVersion;

    // Per-bucket ordered slaughter-priority condition lists (top = keep, bottom = cull).
    public Dictionary<(bool Male, bool Adult), PriorityRuleSet> ruleSets = new()
    {
        { (true, true), new PriorityRuleSet() },
        { (true, false), new PriorityRuleSet() },
        { (false, true), new PriorityRuleSet() },
        { (false, false), new PriorityRuleSet() }
    };

    public static readonly Dictionary<(bool Male, bool Adult), string> ruleSetsNames = new()
    {
        { (true, true), ASMKeys.AdultMales },
        { (true, false), ASMKeys.YoungMales },
        { (false, true), ASMKeys.AdultFemales },
        { (false, false), ASMKeys.YoungFemales }
    };

    public static readonly List<(bool Male, bool Adult)> keys =
    [
        (true, true),
        (true, false),
        (false, true),
        (false, false)
    ];

    /// <summary>Number of rules with validation problems, across all buckets.</summary>
    public int CountProblems() => ruleSets.Values.Sum(set => set.ProblemCount);

    public string? GetProblemsCountsMessage()
    {
        var messages = ruleSets.Keys
            .Select(key => (Key: key, ProblemsCount: ruleSets[key].ProblemCount))
            .Where(x => x.ProblemsCount > 0)
            .Select(x => $"{ruleSetsNames[x.Key].Translate()} ({x.ProblemsCount})")
            .ToList();
        return messages.Count > 0 ? string.Join(", ", messages) : null;
    }

    public List<BasePriorityRule> Get(bool male, bool adult) => ruleSets[(male, adult)].rules;

    public bool HasRules => ruleSets.Values.Any(x => x.HasRules);

    public List<List<string>> Validate(bool male, bool adult) => ruleSets[(male, adult)].Validate();

    public void Add(bool male, bool adult, BasePriorityRule rule) => ruleSets[(male, adult)].Add(rule);

    /// <summary>Opens the add-rule menu of one bucket: the options come from every registered
    /// IRuleOptionsSource, each applying its rule through Add.</summary>
    public void OpenAddRuleMenu(bool male, bool adult, ThingDef animalDef)
    {
        var options = new List<FloatMenuOption>();

        foreach (var source in ServiceRegistry.GetServices<IRuleOptionsSource>())
        {
            source.AddRuleOptions(options, male, adult, animalDef, rule => Add(male, adult, rule));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    public void RemoveAt(bool male, bool adult, int index) => ruleSets[(male, adult)].RemoveAt(index);

    public void CopyAt(bool male, bool adult, int index) => ruleSets[(male, adult)].CopyAt(index);

    public void ChangeVariant(bool male, bool adult, int index) => ruleSets[(male, adult)].ChangeVariant(index);

    public void Move(bool male, bool adult, int from, int to) => ruleSets[(male, adult)].Move(from, to);

    public void ReplaceAll(bool male, bool adult, List<BasePriorityRule> replacement) => ruleSets[(male, adult)].ReplaceAll(replacement);

    public void Clear(bool male, bool adult) => ruleSets[(male, adult)].Clear();

    public void Reset()
    {
        foreach (var ruleSet in ruleSets.Values)
        {
            ruleSet.Reset();
        }
    }

    public PrioritySettingsPreset ToPreset()
    {
        var preset = new PrioritySettingsPreset();

        foreach (var key in keys)
        {
            preset.SetBucket(key, Get(key.Male, key.Adult).Select(r => r.ToPreset()).ToList());
        }

        return preset;
    }

    public void FromPreset(PrioritySettingsPreset preset)
    {
        foreach (var key in keys)
        {
            ruleSets[key].ReplaceAll(BasePriorityRule.FromPresets(preset.GetBucket(key)));
        }
    }

    /// <summary>One bucket's rules as a list preset.</summary>
    public List<BasePriorityRulePreset> ToPreset((bool Male, bool Adult) bucket) =>
        Get(bucket.Male, bucket.Adult).Select(r => r.ToPreset()).ToList();

    public void FromPreset(List<BasePriorityRulePreset> rules, (bool Male, bool Adult) bucket)
    {
        ruleSets[bucket].ReplaceAll(BasePriorityRule.FromPresets(rules));
    }


    public void ExposeData()
    {
        Scribe_Values.Look(ref dataVersion, "dataVersion", 0);

        if (dataVersion == 0)
        {
#pragma warning disable CS0618
            // The v0 save format: one seam into Legacy, converting the flat condition lists.
            foreach (var (key, rules) in LegacyKindPriority.ReadRuleSets())
            {
                ruleSets[key].rules = rules;
            }
#pragma warning restore CS0618
        }
        else if (dataVersion == 1)
        {
            Scribe_Collections.Look(ref ruleSets[(true, true)].rules, "priorityAdultMale", LookMode.Deep);
            Scribe_Collections.Look(ref ruleSets[(true, false)].rules, "priorityYoungMale", LookMode.Deep);
            Scribe_Collections.Look(ref ruleSets[(false, true)].rules, "priorityAdultFemale", LookMode.Deep);
            Scribe_Collections.Look(ref ruleSets[(false, false)].rules, "priorityYoungFemale", LookMode.Deep);
        }

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            foreach (var ruleSet in ruleSets.Values)
            {
                ruleSet.PostLoadInit();
            }

            dataVersion = currentDataVersion;
        }
    }
}
