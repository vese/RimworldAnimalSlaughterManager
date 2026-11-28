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
    // The window's chrome: the frame padding around the table, the presets row pinned to the
    // bottom and the gap above it.
    private const float WindowFrame = 64f;
    private const float WindowHeight = 680f;
    private const float TableTopGap = 12f;
    private const float PresetsRowBottomGap = 2f;

    private readonly ASM_MapComp comp;
    private readonly Map map;
    private Vector2 scroll;
    private int paintCol = -1;   // active paint column for Pregnant/Bonded toggles (-1 = none)
    private bool paintValue;

    public override Vector2 InitialSize => new(SlaughterColumns.TableWidth + WindowFrame, WindowHeight);

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

        // The table fills everything above the presets row.
        var tableBottom = inRect.yMax - UIConstants.ButtonHeight - PresetsRowBottomGap;
        SlaughterTable.Draw(
            new Rect(inRect.x, inRect.y + TableTopGap, inRect.width, tableBottom - inRect.y - TableTopGap),
            comp, map, ref scroll, ref paintCol, ref paintValue);

        if (Widgets.ButtonText(new Rect(inRect.x, tableBottom + PresetsRowBottomGap, inRect.width, UIConstants.ButtonHeight), ASMKeys.ManagePresets.Translate()))
        {
            Find.WindowStack.Add(new SettingsPresetBrowserDialog(comp));
        }
    }
}
