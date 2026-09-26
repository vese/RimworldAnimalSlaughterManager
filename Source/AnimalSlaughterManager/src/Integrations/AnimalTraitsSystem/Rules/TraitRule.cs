using System.Collections;
using System.Linq;
using Verse;

namespace ASM;

/// <summary>
/// A "cull" target: animals of a kind carrying this trait are prioritized for slaughter,
/// optionally scoped by age, sex, and the trait's inheritability.
/// </summary>
public class TraitRule : ITraitRule, IExposable
{
    public HediffDef? trait;
    public AgeScope ageScope = AgeScope.Both;
    public GenderScope genderScope = GenderScope.Any;
    public TraitInheritability inheritMode = TraitInheritability.Both;

    public bool HasNullDef => trait is null;

    public TraitRule() { }
    public TraitRule(HediffDef t) { trait = t; }

    public ITraitRule Copy() => new TraitRule
    {
        trait = trait,
        ageScope = ageScope,
        genderScope = genderScope,
        inheritMode = inheritMode,
    };

    public void SetTrait(HediffDef newTrait)
    {
        trait = newTrait;
    }

    public void SetAgeScope(AgeScope value)
    {
        ageScope = value;
        SettingsChanges.Raise();
    }

    public void SetGenderScope(GenderScope value)
    {
        genderScope = value;
        SettingsChanges.Raise();
    }

    public void SetInheritability(TraitInheritability value)
    {
        inheritMode = value;
        SettingsChanges.Raise();
    }

    public string Label => trait is null ? Constants.MissingLabel : trait.LabelCap;

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref trait, "trait");
        Scribe_Values.Look(ref ageScope, "ageScope", AgeScope.Both);
        Scribe_Values.Look(ref genderScope, "genderScope", GenderScope.Any);
        Scribe_Values.Look(ref inheritMode, "inheritMode", TraitInheritability.Both);
    }
}
