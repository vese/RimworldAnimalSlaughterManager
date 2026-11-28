using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// Global Animal Slaughter Manager as a table that mirrors the vanilla slaughter-management
/// window's look and behavior. Adds Pregnant/Bonded "Забить" toggles, a "Защита" group (by
/// traits / individually), and a Configure button. The table itself lives in
/// Slaughter/Windows/Elements (SlaughterTable + its header and rows).
/// </summary>
public class Dialog_SlaughterManager : Window
{
    private readonly ASM_MapComp comp;
    private readonly Map map;
    private Vector2 scroll;
    private int paintCol = -1;   // active paint column for Pregnant/Bonded toggles (-1 = none)
    private bool paintValue;

    public override Vector2 InitialSize => new Vector2(SlaughterColumns.TableWidth + 64f, 680f);

    public Dialog_SlaughterManager(ASM_MapComp comp)
    {
        this.comp = comp;
        map = Find.CurrentMap;
        doCloseX = true;
        draggable = true;
        resizeable = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        Rect outRect = new Rect(inRect.x, inRect.y + 12f, inRect.width, inRect.height - 52f);
        SlaughterTable.Draw(outRect, comp, map, ref scroll, ref paintCol, ref paintValue);

        if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - 32f, inRect.width, 30f), ASMKeys.ManagePresets.Translate()))
        {
            Find.WindowStack.Add(new SettingsPresetBrowserDialog(comp));
        }
    }
}
