using Verse;

namespace ASM;

public interface ITraitRulesListSection<T> where T : ITraitRule
{
    float Draw(float x, float y, float listWidth, float listHeight, ref ListSectionState listState, ASM_MapComp comp, ThingDef animalDef);
}
