using Verse;

namespace ASM;

public interface ITraitRulesListSection<T> where T : ITraitRule
{
    float Draw(float x, float y, float listWidth, float listHeight, ref ReorderableListState listState, ISettingsProvider comp, ThingDef animalDef);
}
