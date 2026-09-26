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
    /// animal it could match — detected exactly by enumerating animal signal profiles. Duplicates
    /// (identical targets) are additionally reported pairwise for a clearer message.
    /// </summary>
    public List<List<string>> Validate()
    {
        var errors = new List<List<string>?>(rules.Count);

        for (int i = 0; i < rules.Count; i++)
        {
            errors.Add(null);
        }

        for (int i = 0; i < rules.Count - 1; i++)
        {
            for (int j = i + 1; j < rules.Count; j++)
            {
                if (rules[i].Covers(rules[j]) && rules[j].Covers(rules[i]))
                {
                    errors[i] ??= [];
                    errors[j] ??= [];
                    errors[i]!.Add(ASMKeys.ValidationDuplicate.Translate(j + 1));
                    errors[j]!.Add(ASMKeys.ValidationDuplicate.Translate(i + 1));
                }
            }
        }

        var profiles = EnumerateProfiles();
        bool[] reachable = new bool[rules.Count];

        foreach (var profile in profiles)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].MatchesSignals(profile))
                {
                    // The first matching rule wins — it (and only it) is reached by this animal.
                    reachable[i] = true;
                    break;
                }
            }
        }

        for (int i = 0; i < rules.Count; i++)
        {
            if (!reachable[i])
            {
                errors[i] ??= [];
                errors[i]!.Add(ASMKeys.ValidationUnreachable.Translate(JoinUpper(i)));
            }
        }

        return errors!;
    }

    private string JoinUpper(int index) => string.Join(", ", Enumerable.Range(1, index).Select(n => n.ToString()));

    /// <summary>
    /// Every animal signal combination the list's rules can distinguish: training × pregnancy ×
    /// bond × sickness × trait polarity × the specific traits/diseases/skills the rules reference.
    /// A rule is reachable when some profile first-matches it.
    /// </summary>
    private IEnumerable<AnimalSignals> EnumerateProfiles()
    {
        var traits = new HashSet<string>();
        var diseases = new HashSet<string>();
        var skills = new HashSet<string>();
        CollectDefs(traits, diseases, skills);

        // Subsets are built once and shared: matching never mutates them.
        var traitSets = PowerSets(traits).ToArray();
        var diseaseSets = PowerSets(diseases).ToArray();
        var skillSets = PowerSets(skills).ToArray();

        foreach (var training in new[] { TrainingStatus.None, TrainingStatus.Partial, TrainingStatus.Full })
        foreach (var pregnant in new[] { false, true })
        foreach (var bonded in new[] { false, true })
        foreach (var sick in new[] { false, true })
        foreach (var positive in new[] { false, true })
        foreach (var negative in new[] { false, true })
        foreach (var traitSet in traitSets)
        foreach (var diseaseSet in diseaseSets)
        foreach (var skillSet in skillSets)
        {
            yield return new AnimalSignals(pregnant, bonded, sick, positive, negative, training,
                traitSet, diseaseSet, skillSet);
        }
    }

    private void CollectDefs(HashSet<string> traits, HashSet<string> diseases, HashSet<string> skills)
    {
        foreach (var rule in rules)
        {
            switch (rule)
            {
                case TraitPriorityRule t:
                    if (t.trait != null) traits.Add(t.trait.defName);
                    break;
                case DiseasePriorityRule d:
                    if (d.disease != null) diseases.Add(d.disease.defName);
                    break;
                case TrainingPriorityRule tr:
                    if (tr.trainable != null) skills.Add(tr.trainable.defName);
                    break;
            }
        }
    }

    private static IEnumerable<HashSet<string>> PowerSets(HashSet<string> source)
    {
        var items = source.ToArray();

        for (long mask = 0; mask < 1L << items.Length; mask++)
        {
            var set = new HashSet<string>();

            for (int b = 0; b < items.Length; b++)
            {
                if ((mask & 1L << b) != 0)
                {
                    set.Add(items[b]);
                }
            }

            yield return set;
        }
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

    public List<List<string>> Validate(bool male, bool adult) => ruleSets[(male, adult)].Validate();

    public List<BasePriorityRule> Get(bool male, bool adult) => ruleSets[(male, adult)].rules;

    public bool HasRules => ruleSets.Values.Any(x => x.HasRules);

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
