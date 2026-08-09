using HarmonyLib;
using Verse;

namespace ASM.Patches
{
    [StaticConstructorOnStartup]
    public static class ASM_HarmonyInit
    {
        static ASM_HarmonyInit()
        {
            new Harmony("vesilya.animalslaughtermanager").PatchAll();
        }
    }
}
