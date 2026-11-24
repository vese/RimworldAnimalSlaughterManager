
using System.Collections.Generic;
using Verse;

namespace ASM;

public class PriorityRuleSet
{
    public List<BasePriorityRule> rules = [];

    // The validation cache is tagged with the SettingsChangeTracker version it was computed at: any
    // Raise() (ours or any other settings') moves the version and the next read recomputes —
    // no per-owner subscriptions needed.
    private List<List<string>>? validationCache;
    private int validationVersion = -1;

    public bool HasRules => rules != null && rules.Count > 0;

    private bool CacheValid => validationCache != null && validationVersion == SettingsChangeTracker.Version;

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
        SettingsChangeTracker.Raise();
    }

    public void RemoveAt(int index)
    {
        rules.RemoveAt(index);
        SettingsChangeTracker.Raise();
    }

    public void ChangeVariant(int index)
    {
        rules[index].ChangeVariant();
    }

    public void CopyAt(int index)
    {
        rules.Insert(index + 1, rules[index].Clone());
        SettingsChangeTracker.Raise();
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

        SettingsChangeTracker.Raise();
    }

    public void ReplaceAll(List<BasePriorityRule> replacement)
    {
        rules.Clear();
        rules.AddRange(replacement);
        SettingsChangeTracker.Raise();
    }

    public void Clear()
    {
        rules.Clear();
        SettingsChangeTracker.Raise();
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
    /// Cached against the SettingsChangeTracker version — recomputed lazily after any settings change.
    /// </summary>
    public List<List<string>> Validate()
    {
        if (CacheValid)
        {
            return validationCache!;
        }

        validationCache = ComputeValidation();
        validationVersion = SettingsChangeTracker.Version;
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
