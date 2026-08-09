using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ASM
{
    /// <summary>
    /// Replaces the vanilla auto-slaughter list with our recomputed one whenever any of our
    /// customizations are active (age-direction, protected animals, trait breeding targets).
    /// When nothing is customized, the vanilla result is left untouched.
    /// </summary>
    [HarmonyPatch(typeof(AutoSlaughterManager), nameof(AutoSlaughterManager.AnimalsToSlaughter), MethodType.Getter)]
    public static class Patch_AutoSlaughter_List
    {
        public static void Postfix(AutoSlaughterManager __instance, ref List<Pawn> __result)
        {
            var comp = __instance.map?.GetComponent<ASM_MapComp>();
            if (comp == null || !comp.AnyCustomization)
                return;
            if (comp.dirty)
            {
                comp.Recompute(__instance);
                comp.dirty = false;
            }
            __result = comp.cachedList;
        }
    }
}
