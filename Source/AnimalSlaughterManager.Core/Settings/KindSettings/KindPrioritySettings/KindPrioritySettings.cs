
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

public class KindPrioritySettings : IPresettable
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

        foreach (var source in IntegrationServices.GetServices<IRuleOptionsSource>())
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

    public void Save(KindPreset dto)
    {
        dto.SetBucket((true, true), PriorityRuleXml.ToPresets(Get(true, true)));
        dto.SetBucket((true, false), PriorityRuleXml.ToPresets(Get(true, false)));
        dto.SetBucket((false, true), PriorityRuleXml.ToPresets(Get(false, true)));
        dto.SetBucket((false, false), PriorityRuleXml.ToPresets(Get(false, false)));
    }

    /// <summary>One bucket's rules as a list preset.</summary>
    public RuleListPreset ToPreset((bool Male, bool Adult) bucket) =>
        new() { Rules = PriorityRuleXml.ToPresets(Get(bucket.Male, bucket.Adult)) };

    public void FromPreset(RuleListPreset dto, (bool Male, bool Adult) bucket)
    {
        ruleSets[bucket].ReplaceAll(PriorityRuleXml.ToRules(dto.Rules));
    }

    public void Load(KindPreset dto)
    {
        ruleSets[(true, true)].ReplaceAll(PriorityRuleXml.ToRules(dto.GetBucket((true, true))));
        ruleSets[(true, false)].ReplaceAll(PriorityRuleXml.ToRules(dto.GetBucket((true, false))));
        ruleSets[(false, true)].ReplaceAll(PriorityRuleXml.ToRules(dto.GetBucket((false, true))));
        ruleSets[(false, false)].ReplaceAll(PriorityRuleXml.ToRules(dto.GetBucket((false, false))));
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
