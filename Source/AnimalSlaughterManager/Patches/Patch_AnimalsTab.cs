using ASM.Comps;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM.Patches
{
    /// <summary>
    /// Adds a "Slaughter manager" button next to the vanilla "Manage auto-slaughter" button on
    /// the Animals tab, opening the global Animal Slaughter Manager window.
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Animals), nameof(MainTabWindow_Animals.DoWindowContents))]
    public static class Patch_AnimalsTab_Button
    {
        public static void Postfix(Rect rect)
        {
            float x = rect.x + Mathf.Min(rect.width, 260f) + 8f;
            float w = 210f;
            Rect btn = new Rect(x, rect.y, w, 32f);
            if (btn.xMax <= rect.xMax && Widgets.ButtonText(btn, ASMKeys.Manage.Translate()))
            {
                var map = Find.CurrentMap;
                if (map != null)
                    Find.WindowStack.Add(new Dialog_SlaughterManager(map.GetComponent<ASM_MapComp>()));
            }
            TooltipHandler.TipRegion(btn, ASMKeys.ManageDesc.Translate());
        }
    }
}
