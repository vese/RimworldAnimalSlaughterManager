using ASM.Comps;
using RimWorld;
using Verse;

namespace ASM.Columns
{
    /// <summary>Animals-tab checkbox column: toggle an animal's individual protection from
    /// auto-slaughter (ASM_MapComp.protectedPawnIDs). Header uses the shield texture
    /// (PawnColumnDef.headerIcon = UI/Icons/ASM/Shield).</summary>
    public class PawnColumnWorker_IndivProtection : PawnColumnWorker_Checkbox
    {
        protected override bool HasCheckbox(Pawn pawn)
            => pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer && pawn.SpawnedOrAnyParentSpawned;

        protected override bool GetValue(Pawn pawn)
            => pawn.MapHeld?.GetComponent<ASM_MapComp>()?.IsProtected(pawn) ?? false;

        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            var comp = pawn.MapHeld?.GetComponent<ASM_MapComp>();
            if (comp != null && value != comp.IsProtected(pawn))
                comp.ToggleProtected(pawn);
        }

        protected override string GetTip(Pawn pawn) => ASMKeys.ProtectHeaderTip.Translate();

        protected override string GetHeaderTip(PawnTable table) => ASMKeys.ProtectHeaderTip.Translate();
    }
}
