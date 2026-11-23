using HarmonyLib;
using Verse;

namespace ASM
{
    [StaticConstructorOnStartup]
    public static class ASM_HarmonyInit
    {
        static ASM_HarmonyInit()
        {
            var harmony = new Harmony("vesilya.animalslaughtermanager");
            // PatchAll() without arguments only patches the calling assembly; the patches
            // themselves live in Core.
            harmony.PatchAll();
            harmony.PatchAll(typeof(ASM_MapComp).Assembly);
        }
    }
}
