using ASM.Comps;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ASM.Patches
{
    /// <summary>
    /// Mirror the vanilla auto-slaughter dirty events so our cached list recomputes on the
    /// same triggers (pawn spawn/despawn/faction change, config change).
    /// </summary>
    public static class Patch_AutoSlaughter_Notify
    {
        private static void Mark(AutoSlaughterManager mgr) => mgr?.map?.GetComponent<ASM_MapComp>()?.MarkDirty();

        [HarmonyPatch(typeof(AutoSlaughterManager), nameof(AutoSlaughterManager.Notify_PawnDespawned))]
        public static class Notify_PawnDespawned
        {
            public static void Postfix(AutoSlaughterManager __instance) => Mark(__instance);
        }

        [HarmonyPatch(typeof(AutoSlaughterManager), nameof(AutoSlaughterManager.Notify_PawnSpawned))]
        public static class Notify_PawnSpawned
        {
            public static void Postfix(AutoSlaughterManager __instance) => Mark(__instance);
        }

        [HarmonyPatch(typeof(AutoSlaughterManager), nameof(AutoSlaughterManager.Notify_PawnChangedFaction))]
        public static class Notify_PawnChangedFaction
        {
            public static void Postfix(AutoSlaughterManager __instance) => Mark(__instance);
        }

        [HarmonyPatch(typeof(AutoSlaughterManager), nameof(AutoSlaughterManager.Notify_ConfigChanged))]
        public static class Notify_ConfigChanged
        {
            public static void Postfix(AutoSlaughterManager __instance) => Mark(__instance);
        }
    }
}
