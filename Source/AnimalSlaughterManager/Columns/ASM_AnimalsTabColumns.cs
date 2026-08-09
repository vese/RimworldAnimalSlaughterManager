using RimWorld;
using Verse;

namespace ASM.Columns
{
    /// <summary>Inserts ASM's protection + slaughter-status columns into the vanilla Animals/Pets
    /// tab table, right after the vanilla Slaughter column.</summary>
    [StaticConstructorOnStartup]
    public static class ASM_AnimalsTabColumns
    {
        static ASM_AnimalsTabColumns()
        {
            var animals = PawnTableDefOf.Animals;
            var protection = DefDatabase<PawnColumnDef>.GetNamedSilentFail("ASM_IndivProtection");
            if (protection == null || animals.columns.Contains(protection))
                return;
            int idx = animals.columns.FindIndex(c => c.workerClass == typeof(PawnColumnWorker_Slaughter));
            if (idx < 0)
                idx = animals.columns.Count - 1;
            animals.columns.Insert(idx + 1, protection);
        }
    }
}
