using Verse;

namespace ASM;

/// <summary>
/// A breeding ("keep") target: keep a number of animals of a kind that carry an ATS trait,
/// optionally scoped by age, sex, and the trait's inheritability. When inheritable, the
/// selection biases toward keeping a breeding pair.
/// </summary>
public class TraitProtectRule : IExposable
{
    public HediffDef? trait;
    public int keepCount = 1;
    public AgeScope ageScope = AgeScope.Both;
    public GenderScope genderScope = GenderScope.Any;
    public TraitInheritability inheritMode = TraitInheritability.Both;

    public TraitProtectRule() { }
    public TraitProtectRule(HediffDef t) { trait = t; }

    public string Label => trait != null ? trait.LabelCap : Constants.MissingLabel;

    public void ExposeData()
    {
        Scribe_Defs.Look(ref trait, "trait");
        Scribe_Values.Look(ref keepCount, "keepCount", 1);
        Scribe_Values.Look(ref ageScope, "ageScope", AgeScope.Both);
        Scribe_Values.Look(ref genderScope, "genderScope", GenderScope.Any);
        Scribe_Values.Look(ref inheritMode, "inheritMode", TraitInheritability.Both);
    }
}
