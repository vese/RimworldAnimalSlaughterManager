using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// Per-animal-kind slaughter settings, in two tabs:
/// • Priorities — sex×age (older/younger) buckets and cull/spare trait priorities.
/// • Special rules — breeding ("keep") protection traits and force-slaughter traits
///   (slaughter regardless of count/limits; protection still wins).
///
/// The header carries the animal icon/name plus the per-kind preset button and the two reset
/// buttons (reset this tab / reset all tabs) in one row, to the right of the name. Each trait
/// section has, to the right of its title, a clear-list button (empties the whole list), the
/// add-trait button, and a single list-preset button (which both saves and loads — one window
/// covers export and import). Trait rows have a drag handle, a copy button, and
/// age/gender/inheritable columns, so a list configured for one kind can be saved and loaded
/// into another. The trait lists expand to fill the window's remaining height (resizing with the
/// window), with a 150px minimum; below that the whole window scrolls. Blocks (preferences, each
/// trait section) are separated by divider lines. The window is not draggable as a whole (that
/// made row reorder move the window); it drags from its frame and the header/tab band instead.
/// </summary>
public class Dialog_KindSlaughterSettings : Window
{
    private const float WindowWidth = 1000f;

    private readonly ASM_MapComp comp;
    private readonly ThingDef animalDef;
    private readonly KindSettings settings;
    private readonly List<IKindSlaughterSettingsDialogTab> tabs;
    private IKindSlaughterSettingsDialogTab currentTab;

    public override Vector2 InitialSize => new(WindowWidth, Screen.height * 0.85f);

    public Dialog_KindSlaughterSettings(ASM_MapComp comp, ThingDef animalDef)
    {
        this.comp = comp;
        this.animalDef = animalDef;
        settings = comp.GetSettings(animalDef);
        doCloseX = true;
        // Not draggable as a whole: a draggable window ends OnGUI with GUI.DragWindow() (no args),
        // which grabs any unclaimed press — so reordering a row in the shorter/empty second list
        // (spare/forceCull) moved the window instead. The window drags from its frame + the
        // header/tab band (LateWindowOnGUI); the lists never start a window drag, so all four
        // lists reorder cleanly.
        draggable = false;
        resizeable = true;
        tabs =
        [
            new KindSlaughterSettingsDialogPrioritiesTab(comp, settings),
            new KindSlaughterSettingsDialogSpecialRulesTab(settings.traitsSettings),
            new KindSlaughterSettingsDialogGeneralTab(comp, comp.globalSettings)
        ];
        currentTab = tabs.First();
    }

    public override void PreClose()
    {
        base.PreClose();

        var msg = ASMKeys.ValidationProblems.Translate(animalDef.LabelCap, settings.prioritySettings.GetErrorsCountsMessage());

        if (msg != null)
        {
            Messages.Message(msg, MessageTypeDefOf.NegativeEvent, false);
            Find.WindowStack.Add(new Dialog_MessageBox(msg));
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        currentTab.DoWindowContents(inRect, comp, animalDef, settings, DrawTabBar);
    }

    // The window is not draggable as a whole (see ctor). Drag instead from the frame on all four
    // sides plus the header/tab band — never from the trait lists, so row reorder is never stolen.
    protected override void LateWindowOnGUI(Rect inRect)
    {
        var width = inRect.xMax - inRect.x;
        var height = inRect.yMax + inRect.y;
        GUI.DragWindow(new Rect(inRect.x, inRect.y, width, BaseKindSlaughterSettingsTab.HeaderHeight + BaseKindSlaughterSettingsTab.TabBarHeight + UIConstants.GapY + UIConstants.GapY)); // top frame
        // TODO
        //GUI.DragWindow(new Rect(0, winH - m, winW, m));                             // bottom frame
        //GUI.DragWindow(new Rect(0, 0, m, winH));                                    // left frame
        //GUI.DragWindow(new Rect(winW - m, 0, m, winH));                             // right frame
        //GUI.DragWindow(new Rect(inRect.x, inRect.y, inRect.width, HeaderH + TabBarH)); // header + tab strip
    }

    private void DrawTabBar(float x, float y, float width, float height) => TabDrawer.DrawTabs(
        new Rect(x, y, width, height),
        [.. tabs.Select(tab => new TabRecord(tab.Name, () => currentTab = tab, currentTab == tab))]);
}
