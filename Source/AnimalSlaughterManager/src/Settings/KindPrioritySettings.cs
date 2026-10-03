using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

public class PriorityRuleSet
{
    public List<BasePriorityRule> rules = [];

    // The validation cache is tagged with the SettingsChanges version it was computed at: any
    // Raise() (ours or any other settings') moves the version and the next read recomputes —
    // no per-owner subscriptions needed.
    private List<List<string>>? validationCache;
    private int validationVersion = -1;

    public bool HasRules => rules != null && rules.Count > 0;

    private bool CacheValid => validationCache != null && validationVersion == SettingsChanges.Version;

    /// <summary>Number of rules with validation problems in this bucket.</summary>
    public int ProblemCount
    {
        get
        {
            var count = 0;

            foreach (var problems in Validate())
            {
                if (problems is not null && problems.Count > 0)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public void Reset()
    {
        rules.Clear();
        validationVersion = -1;
    }

    public void Add(BasePriorityRule rule)
    {
        rules.Add(rule);
        SettingsChanges.Raise();
    }

    public void RemoveAt(int index)
    {
        rules.RemoveAt(index);
        SettingsChanges.Raise();
    }

    public void ChangeVariant(int index)
    {
        rules[index].ChangeVariant();
    }

    public void CopyAt(int index)
    {
        rules.Insert(index + 1, rules[index].Clone());
        SettingsChanges.Raise();
    }

    public void Move(int from, int to)
    {
        if (from < 0 || to < 0 || from == to || from >= rules.Count || to > rules.Count)
        {
            return;
        }

        var rule = rules[from];
        rules.RemoveAt(from);

        if (from < to)
        {
            rules.Insert(to - 1, rule);
        }
        else
        {
            rules.Insert(to, rule);
        }

        SettingsChanges.Raise();
    }

    public void ReplaceAll(List<BasePriorityRule> replacement)
    {
        rules.Clear();
        rules.AddRange(replacement);
        SettingsChanges.Raise();
    }

    public void Clear()
    {
        rules.Clear();
        SettingsChanges.Raise();
    }

    public void PostLoadInit()
    {
        if (rules is null)
        {
            rules = [];
        }
        else
        {
            // A trait/disease/trainable def may resolve to null when the mod that defined it
            // (e.g. Animal Traits System) was disabled on this save. Drop those dead entries so
            // the settings don't fill up with no-op "?" rows. (This does not silence RimWorld's
            // own "Could not load reference" log for hediffs still on the pawns — that is the
            // base game resolving the save, outside this mod's control.)
            rules.RemoveAll(c => c is null || c.HasNullDef);
        }

        validationVersion = -1;
    }

    /// <summary>
    /// Per-rule problem messages (null when the rule is fine). The pass asks each rule to
    /// accumulate into trait sets (creating-or-updating its set, registering validators); then
    /// every registered validator runs once over the accumulated data. Rules are matched
    /// top-to-down, the first match wins — see SetClosureValidator and DuplicateValidator.
    /// Cached against the SettingsChanges version — recomputed lazily after any settings change.
    /// </summary>
    public List<List<string>> Validate()
    {
        if (CacheValid)
        {
            return validationCache!;
        }

        validationCache = ComputeValidation();
        validationVersion = SettingsChanges.Version;
        return validationCache;
    }

    private List<List<string>> ComputeValidation()
    {
        var problems = new List<List<string>?>(rules.Count);

        for (int i = 0; i < rules.Count; i++)
        {
            problems.Add(null);
        }

        void AddProblem(int index, string message)
        {
            problems[index] ??= [];
            problems[index]!.Add(message);
        }

        var context = new RuleValidationContext();

        for (int i = 0; i < rules.Count; i++)
        {
            context.Add(rules[i], i);
        }

        foreach (var validator in context.Validators)
        {
            validator.Validate(context, AddProblem);
        }

        return problems!;
    }

}

public class KindPrioritySettings : IPresettable
{
    private const int currentDataVersion = 1;

    // New instances start at the current version so they serialize in the current format;
    // loading an old save overwrites this with the saved version and migrates in PostLoadInit.
    private int dataVersion = currentDataVersion;
    private KindPrioritySettingsLegacy? settingsLegacy;

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

    public void Save(KindDto dto)
    {
        dto.PrioRulesAdultMale = Get(true, true).Select(PriorityRuleDto.From).ToList();
        dto.PrioRulesYoungMale = Get(true, false).Select(PriorityRuleDto.From).ToList();
        dto.PrioRulesAdultFemale = Get(false, true).Select(PriorityRuleDto.From).ToList();
        dto.PrioRulesYoungFemale = Get(false, false).Select(PriorityRuleDto.From).ToList();
    }

    public void Load(KindDto dto)
    {
        LoadRuleSet(dto.PrioRulesAdultMale, dto.PrioAdultMale, ruleSets[(true, true)]);
        LoadRuleSet(dto.PrioRulesYoungMale, dto.PrioYoungMale, ruleSets[(true, false)]);
        LoadRuleSet(dto.PrioRulesAdultFemale, dto.PrioAdultFemale, ruleSets[(false, true)]);
        LoadRuleSet(dto.PrioRulesYoungFemale, dto.PrioYoungFemale, ruleSets[(false, false)]);
    }

    // PrioRules* — current format; the legacy ConditionDto lists cover presets saved before
    // the priority-rule refactor.
    private static void LoadRuleSet(List<PriorityRuleDto> rules, List<ConditionDto> legacy, PriorityRuleSet target)
    {
        var loaded = new List<BasePriorityRule>();

        if (rules != null)
        {
            foreach (var r in rules)
            {
                var rule = r.ToRule();

                if (rule != null)
                {
                    loaded.Add(rule);
                }
            }
        }
        else if (legacy != null)
        {
#pragma warning disable CS0618
            foreach (var c in legacy)
            {
                var rule = KindPrioritySettingsLegacy.Convert(c.ToCondition());

                if (rule != null)
                {
                    loaded.Add(rule);
                }
            }
#pragma warning restore CS0618
        }

        target.ReplaceAll(loaded);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref dataVersion, "dataVersion", 0);

        if (dataVersion == 0)
        {
            settingsLegacy = new KindPrioritySettingsLegacy();
            settingsLegacy.ExposeData();
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
            if (dataVersion == 0)
            {
                ruleSets[(true, true)].rules = settingsLegacy!.PriorityAdultMale;
                ruleSets[(true, false)].rules = settingsLegacy!.PriorityYoungMale;
                ruleSets[(false, true)].rules = settingsLegacy!.PriorityAdultFemale;
                ruleSets[(false, false)].rules = settingsLegacy!.PriorityYoungFemale;
                
                settingsLegacy.Reset();
            }

            foreach (var ruleSet in ruleSets.Values)
            {
                ruleSet.PostLoadInit();
            }

            dataVersion = currentDataVersion;
        }
    }
}
