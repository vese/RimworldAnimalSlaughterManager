using Verse;

namespace ASM;

public interface ITraitRule
{
    bool HasNullDef { get; }
    ITraitRule Copy();
    void SetTrait(HediffDef t);
}
