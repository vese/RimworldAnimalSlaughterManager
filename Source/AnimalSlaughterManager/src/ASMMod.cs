using RimWorld;
using UnityEngine;
using Verse;

namespace ASM
{
    /// <summary>
    /// Mod entry point. Registers settings and the Harmony patches.
    /// </summary>
    public class ASMMod : Mod
    {
        public static ASMSettings Settings;

        public ASMMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ASMSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);
            list.CheckboxLabeled(ASMKeys.SettingLogging.Translate(), ref Settings.enableLogging, ASMKeys.SettingLoggingDesc.Translate());
            list.End();
        }

        public override string SettingsCategory() => "Animal Slaughter Manager";
    }

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
}
