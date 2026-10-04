using HarmonyLib;
using Verse;

namespace ASM
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
