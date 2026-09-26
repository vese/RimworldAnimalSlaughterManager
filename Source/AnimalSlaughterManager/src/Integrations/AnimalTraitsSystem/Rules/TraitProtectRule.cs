using Verse;

namespace ASM;

/// <summary>
/// A breeding ("keep") target: keep a number of animals of a kind that carry an ATS trait,
/// optionally scoped by age, sex, and the trait's inheritability. When inheritable, the
/// selection biases toward keeping a breeding pair.
/// </summary>
public class TraitProtectRule : ITraitRule, IExposable
{
    public HediffDef? trait;
    public int keepCount = 1;
    public AgeScope ageScope = AgeScope.Both;
    public GenderScope genderScope = GenderScope.Any;
    public TraitInheritability inheritMode = TraitInheritability.Both;

    public bool HasNullDef => trait is null;

    public TraitProtectRule() { }
    public TraitProtectRule(HediffDef t) { trait = t; }

    public ITraitRule Copy() => new TraitProtectRule
    {
        trait = trait,
        keepCount = keepCount,
        ageScope = ageScope,
        genderScope = genderScope,
        inheritMode = inheritMode,
    };

    public void SetTrait(HediffDef newTrait)
    {
        trait = newTrait;
    }

    public string Label => trait is null ? Constants.MissingLabel : trait.LabelCap;

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref trait, "trait");
        Scribe_Values.Look(ref keepCount, "keepCount", 1);
        Scribe_Values.Look(ref ageScope, "ageScope", AgeScope.Both);
        Scribe_Values.Look(ref genderScope, "genderScope", GenderScope.Any);
        Scribe_Values.Look(ref inheritMode, "inheritMode", TraitInheritability.Both);
    }
}
