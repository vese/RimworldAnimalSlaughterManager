using System;
#pragma warning disable CS0612, CS0618
using Verse;

namespace ASM;

[Obsolete]
public interface ITraitRule
{
    bool HasNullDef { get; }
    ITraitRule Copy();
    void SetTrait(HediffDef t);
}
