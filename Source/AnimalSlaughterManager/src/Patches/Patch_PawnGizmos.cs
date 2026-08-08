using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM
{
    /// <summary>
    /// Adds the "Protect from slaughter" toggle and the per-kind "Slaughter settings" button to
    /// player ANIMALS only. The gate (<see cref="AutoSlaughterManager.CanEverAutoSlaughter"/>)
    /// requires HomeFaction == player and a non-Dryad animal, so these never appear on human
    /// colonists.
    /// </summary>
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_PawnGizmos
    {
        private static Texture2D _protectIcon;
        private static Texture2D _settingsIcon;
        private static Texture2D ProtectIcon => _protectIcon ?? (_protectIcon = ContentFinder<Texture2D>.Get("UI/Icons/ASM/Shield"));
        private static Texture2D SettingsIcon => _settingsIcon ?? (_settingsIcon = ContentFinder<Texture2D>.Get("UI/Icons/ASM/Settings"));

        // Pre-load the gizmo icons on the main thread during mod init. Satisfies RimWorld's
        // StaticConstructorOnStartup asset-loading requirement (avoids the "probably needs a
        // StaticConstructorOnStartup attribute" warning for the Texture2D fields above).
        static Patch_PawnGizmos()
        {
            _protectIcon = ContentFinder<Texture2D>.Get("UI/Icons/ASM/Shield");
            _settingsIcon = ContentFinder<Texture2D>.Get("UI/Icons/ASM/Settings");
        }

        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance == null || !__instance.Spawned || __instance.Map == null)
                return;
            // Animals only — never human colonists. (CanEverAutoSlaughter alone would match
            // player-faction humans too, since it only checks faction and Dryad.)
            if (__instance.RaceProps == null || !__instance.RaceProps.Animal)
                return;
            if (!AutoSlaughterManager.CanEverAutoSlaughter(__instance))
                return;

            var comp = __instance.Map.GetComponent<ASM_MapComp>();
            var def = __instance.def;
            bool isProtected = comp.IsProtected(__instance);
            __result = __result.Concat(new Gizmo[]
            {
                new Command_Toggle
                {
                    defaultLabel = (isProtected ? "ASM.Unprotect" : "ASM.Protect").Translate(),
                    defaultDesc = "ASM.ProtectedDesc".Translate(),
                    icon = ProtectIcon,
                    isActive = () => comp.IsProtected(__instance),
                    toggleAction = () => comp.ToggleProtected(__instance),
                },
                new Command_Action
                {
                    defaultLabel = "ASM.Settings".Translate(),
                    defaultDesc = "ASM.SettingsDesc".Translate(),
                    icon = SettingsIcon,
                    action = () => Find.WindowStack.Add(new Dialog_KindSlaughterSettings(comp, def)),
                }
            });
        }
    }
}
