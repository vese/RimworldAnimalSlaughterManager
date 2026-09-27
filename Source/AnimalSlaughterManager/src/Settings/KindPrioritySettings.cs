using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

public class PriorityRuleSet
{
    public List<BasePriorityRule> rules = [];

    public bool HasRules => rules != null && rules.Count > 0;

    public void Reset()
    {
        rules.Clear();
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
    }

    /// <summary>
    /// Per-rule problem messages (null when the rule is fine). Rules are matched top-to-down and
    /// the first match wins, so a rule is unreachable when the rules above it together match every
    /// animal it could match — detected exactly by enumerating the states of every rule axis the
    /// list uses. Duplicates (identical targets) are additionally reported pairwise for a clearer
    /// message.
    /// </summary>
    /// <summary>
    /// Per-rule problem messages (null when the rule is fine). The pass asks each rule to
    /// accumulate into trait sets (creating-or-updating its set, registering validators); then
    /// every registered validator runs once over the accumulated data. Rules are matched
    /// top-to-down, the first match wins — see ReachabilityValidator and DuplicateValidator.
    /// </summary>
    public List<List<string>> Validate()
    {
        var errors = new List<List<string>?>(rules.Count);

        for (int i = 0; i < rules.Count; i++)
        {
            errors.Add(null);
        }

        var context = new RuleValidationContext();

        for (int i = 0; i < rules.Count; i++)
        {
            context.Add(rules[i], i);
        }

        foreach (var validator in context.Validators)
        {
            validator.Validate(context, rules, errors);
        }

        return errors!;
    }

}

public class KindPrioritySettings : IPresettable
{
    private const int currentDataVersion = 1;

    private int dataVersion = 0;
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

    public string? GetErrorsCountsMessage()
    {
        var messages = ruleSets.Keys
            .Select(key => (Key: key, ErrorsCount: ruleSets[key].Validate().Count(x => x is not null)))
            .Where(x => x.ErrorsCount > 0)
            .Select(x => $"{ruleSetsNames[x.Key].Translate()} ({x.ErrorsCount})")
            .ToList();
        return messages.Count > 0 ? string.Join(", ", messages) : null;
    }



    public List<BasePriorityRule> Get(bool male, bool adult) => ruleSets[(male, adult)].rules;

    public bool HasRules => ruleSets.Values.Any(x => x.HasRules);

    public List<List<string>> Validate(bool male, bool adult) => ruleSets[(male, adult)].Validate();

    public void Add(bool male, bool adult, BasePriorityRule rule) => Get(male, adult).Add(rule);

    public void RemoveAt(bool male, bool adult, int index) => Get(male, adult).RemoveAt(index);

    public void CopyAt(bool male, bool adult, int index) => ruleSets[(male, adult)].CopyAt(index);

    public void Move(bool male, bool adult, int from, int to) => ruleSets[(male, adult)].Move(from, to);

    public void ReplaceAll(bool male, bool adult, List<BasePriorityRule> replacement) => ruleSets[(male, adult)].ReplaceAll(replacement);

    public void Clear(bool male, bool adult) => Get(male, adult).Clear();

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
        LoadRuleSet(dto.PrioRulesAdultMale, dto.PrioAdultMale, Get(true, true));
        LoadRuleSet(dto.PrioRulesYoungMale, dto.PrioYoungMale, Get(true, false));
        LoadRuleSet(dto.PrioRulesAdultFemale, dto.PrioAdultFemale, Get(false, true));
        LoadRuleSet(dto.PrioRulesYoungFemale, dto.PrioYoungFemale, Get(false, false));
    }

    // PrioRules* — current format; the legacy ConditionDto lists cover presets saved before
    // the priority-rule refactor.
    private static void LoadRuleSet(List<PriorityRuleDto> rules, List<ConditionDto> legacy, List<BasePriorityRule> target)
    {
        target.Clear();

        if (rules != null)
        {
            foreach (var r in rules)
            {
                var rule = r.ToRule();

                if (rule != null)
                {
                    target.Add(rule);
                }
            }

            return;
        }

        if (legacy != null)
        {
#pragma warning disable CS0618
            foreach (var c in legacy)
            {
                var rule = KindPrioritySettingsLegacy.Convert(c.ToCondition());

                if (rule != null)
                {
                    target.Add(rule);
                }
            }
#pragma warning restore CS0618
        }
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
