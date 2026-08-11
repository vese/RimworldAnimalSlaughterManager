using Verse;

namespace ASM;

/// <summary>
/// Mod settings, serialized to the save folder. Currently only the debug-logging toggle.
/// </summary>
public class ASMSettings : ModSettings
{
    public bool enableLogging = false;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref enableLogging, "enableLogging", false);
        base.ExposeData();
    }
}
