using Verse;

namespace ASM;

/// <summary>
/// A "cull" target: animals of a kind carrying this trait are prioritized for slaughter,
/// optionally scoped by age, sex, and the trait's inheritability.
/// </summary>
public class TraitRule : IExposable
{
    public HediffDef? trait;
    public AgeScope ageScope = AgeScope.Both;
    public GenderScope genderScope = GenderScope.Any;
    public TraitInheritability inheritMode = TraitInheritability.Both;

    public TraitRule() { }
    public TraitRule(HediffDef t) { trait = t; }

    public string Label => trait != null ? trait.LabelCap : Constants.MissingLabel;

    public void ExposeData()
    {
        Scribe_Defs.Look(ref trait, "trait");
        Scribe_Values.Look(ref ageScope, "ageScope", AgeScope.Both);
        Scribe_Values.Look(ref genderScope, "genderScope", GenderScope.Any);
        Scribe_Values.Look(ref inheritMode, "inheritMode", TraitInheritability.Both);
    }
}
