using HarmonyLib;
using Verse;

namespace ASM;

[StaticConstructorOnStartup]
public static class ASM_HarmonyInit
{
    static ASM_HarmonyInit()
    {
        var harmony = new Harmony("vesilya.animalslaughtermanager");
        harmony.PatchAll(typeof(ASM_MapComp).Assembly);
    }
}
