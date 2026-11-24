using System.Linq;
using Verse;

namespace ASM;

/// <summary>One entry of the kind's special-rules lists (keep or force-cull): an
/// integration-defined target def, scoped by age and sex, plus the integration's own
/// inheritability value carried as a serialized name this core does not parse.</summary>
public abstract class SpecialRule : IExposable
{
    public Def? target;

    public AgeScope ageScope = AgeScope.Both;
    public GenderScope genderScope = GenderScope.Any;

    // Serialized name of the owning integration's inheritability value (an ATS enum name);
    // this core does not parse it.
    public string inheritMode = "";

    // Save format: the def's name, resolved across all def databases after loading —
    // Scribe_Defs needs the target's concrete def database, which the core does not know.
    private string targetDefName = "";

    public bool HasNullDef => target is null;

    public string Label => target is null ? Constants.MissingLabel : target.LabelCap;

    public abstract SpecialRule Copy();

    public void SetTarget(Def newTarget)
    {
        target = newTarget;
        SettingsChangeTracker.Raise();
    }

    public void SetAgeScope(AgeScope value)
    {
        ageScope = value;
        SettingsChangeTracker.Raise();
    }

    public void SetGenderScope(GenderScope value)
    {
        genderScope = value;
        SettingsChangeTracker.Raise();
    }

    public void SetInheritMode(string value)
    {
        inheritMode = value;
        SettingsChangeTracker.Raise();
    }

    public virtual void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            targetDefName = target?.defName ?? "";
        }

        Scribe_Values.Look(ref targetDefName, "target", "");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            target = GenDefDatabase.GetAllDefsInDatabaseForDef(typeof(Def)).FirstOrDefault(d => d.defName == targetDefName);
        }

        Scribe_Values.Look(ref ageScope, "ageScope", AgeScope.Both);
        Scribe_Values.Look(ref genderScope, "genderScope", GenderScope.Any);
        Scribe_Values.Look(ref inheritMode, "inheritMode", "");
    }
}
