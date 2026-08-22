using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ASM;

//TODO: метод валидации списков
//TODO: контроллер с событиями изменения, по которому ревалидировать
//TODO: для каждого списка отдельный класс с добавлением/удалением/изменением и ревалидацией

public class PriorityRuleSet
{
    public List<BasePriorityRule> rules = [];

    public bool HasRules => rules != null && rules.Count > 0;

    public void Reset()
    {
        rules.Clear();
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

    public List<List<string>> Validate()
    {
        if (!HasRules)
        {
            return [];
        }

        var errors = new List<List<string>>(rules.Count);

        for (int i = 0; i < rules.Count - 1; i++)
        {
            var first = rules[i];

            for (int j = i + 1; j < rules.Count; j++)
            {
                var second = rules[j];

                if (first.IsInvalid(second))
                {
                    errors[i] ??= [];
                    errors[j] ??= [];
                    // TODO: errors messages
                    errors[i]?.Add("Invalid");
                    errors[j]?.Add("Invalid");
                }
            }
        }

        return errors;
    }
}

public class KindPrioritySettings
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

    public List<BasePriorityRule> GetPriorityRules(bool male, bool adult) => ruleSets[(male, adult)].rules;

    public bool HasRules => ruleSets.Values.All(x => x.HasRules);

    public void Reset()
    {
        foreach (var ruleSet in ruleSets.Values)
        {
            ruleSet.Reset();
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
