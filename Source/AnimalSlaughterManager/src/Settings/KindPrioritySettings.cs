using System.Collections.Generic;
using Verse;

namespace ASM;

public class KindPrioritySettings
{
    private const int currentDataVersion = 1;

    private int dataVersion = 0;
    private KindPrioritySettingsLegacy? settingsLegacy;

    // Per-bucket ordered slaughter-priority condition lists (top = keep, bottom = cull).
    public List<BasePriorityRule> priorityAdultMale = [];
    public List<BasePriorityRule> priorityYoungMale = [];
    public List<BasePriorityRule> priorityAdultFemale = [];
    public List<BasePriorityRule> priorityYoungFemale = [];

    public List<BasePriorityRule> GetPriorityRules(bool male, bool adult) =>
        male ? (adult ? priorityAdultMale : priorityYoungMale) : (adult ? priorityAdultFemale : priorityYoungFemale);

    public bool AnyPriorityConditions() =>
        priorityAdultMale != null && priorityAdultMale.Count > 0 ||
        priorityYoungMale != null && priorityYoungMale.Count > 0 ||
        priorityAdultFemale != null && priorityAdultFemale.Count > 0 ||
        priorityYoungFemale != null && priorityYoungFemale.Count > 0;

    /// <summary>True when this kind deviates from vanilla behaviour and must be recomputed.</summary>
    public bool Customized => AnyPriorityConditions();

    public void Reset()
    {
        priorityAdultMale.Clear();
        priorityYoungMale.Clear();
        priorityAdultFemale.Clear();
        priorityYoungFemale.Clear();
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
            Scribe_Collections.Look(ref priorityAdultMale, "priorityAdultMale", LookMode.Deep);
            Scribe_Collections.Look(ref priorityYoungMale, "priorityYoungMale", LookMode.Deep);
            Scribe_Collections.Look(ref priorityAdultFemale, "priorityAdultFemale", LookMode.Deep);
            Scribe_Collections.Look(ref priorityYoungFemale, "priorityYoungFemale", LookMode.Deep);
        }

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (dataVersion == 0)
            {
                priorityAdultMale = settingsLegacy!.PriorityAdultMale;
                priorityYoungMale = settingsLegacy!.PriorityYoungMale;
                priorityAdultFemale = settingsLegacy!.PriorityAdultFemale;
                priorityYoungFemale = settingsLegacy!.PriorityYoungFemale;
                
                settingsLegacy.Reset();
            }

            priorityAdultMale ??= [];
            priorityYoungMale ??= [];
            priorityAdultFemale ??= [];
            priorityYoungFemale ??= [];

            // A trait/disease/trainable def may resolve to null when the mod that defined it
            // (e.g. Animal Traits System) was disabled on this save. Drop those dead entries so
            // the settings don't fill up with no-op "?" rows. (This does not silence RimWorld's
            // own "Could not load reference" log for hediffs still on the pawns — that is the
            // base game resolving the save, outside this mod's control.)
            priorityAdultMale?.RemoveAll(c => c == null || c.HasNullDef);
            priorityYoungMale?.RemoveAll(c => c == null || c.HasNullDef);
            priorityAdultFemale?.RemoveAll(c => c == null || c.HasNullDef);
            priorityYoungFemale?.RemoveAll(c => c == null || c.HasNullDef);

            dataVersion = currentDataVersion;
        }
    }
}
