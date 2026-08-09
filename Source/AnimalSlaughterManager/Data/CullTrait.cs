using RimWorld;
using Verse;

namespace ASM.Data
{
    /// <summary>
    /// A "cull" target: animals of a kind carrying this trait are prioritized for slaughter,
    /// optionally scoped by age, sex, and the trait's inheritability.
    /// </summary>
    public class CullTrait : IExposable
    {
        public HediffDef trait;
        public AgeScope ageScope = AgeScope.Both;
        public GenderScope genderScope = GenderScope.Any;
        public InheritableFilter inheritMode = InheritableFilter.Both;

        public CullTrait() { }
        public CullTrait(HediffDef t) { trait = t; }

        public string Label => trait != null ? trait.LabelCap : "???";

        public void ExposeData()
        {
            Scribe_Defs.Look(ref trait, "trait");
            Scribe_Values.Look(ref ageScope, "ageScope", AgeScope.Both);
            Scribe_Values.Look(ref genderScope, "genderScope", GenderScope.Any);
            Scribe_Values.Look(ref inheritMode, "inheritMode", InheritableFilter.Both);
        }
    }
}
