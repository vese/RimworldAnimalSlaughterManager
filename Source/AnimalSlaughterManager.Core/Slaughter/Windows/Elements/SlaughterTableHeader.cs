using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>The table's two-row header: group titles on top, sub-columns with hover tooltips
/// below, gray gridlines at the groups' edges.</summary>
internal static class SlaughterTableHeader
{
    // (startX, width, topTitleKey). Name group has no top title.
    private static readonly (float X, float Width, string TitleKey)[] Groups =
    {
        (SlaughterColumns.XTotC, SlaughterColumns.XTotM + SlaughterColumns.WCol - SlaughterColumns.XTotC, "AutoSlaugtherHeaderColTotal"),
        (SlaughterColumns.XAMC, SlaughterColumns.XAMM + SlaughterColumns.WCol - SlaughterColumns.XAMC, "AnimalMaleAdult"),
        (SlaughterColumns.XYMC, SlaughterColumns.XYMM + SlaughterColumns.WCol - SlaughterColumns.XYMC, "AnimalMaleYoung"),
        (SlaughterColumns.XAFC, SlaughterColumns.XAFM + SlaughterColumns.WCol - SlaughterColumns.XAFC, "AnimalFemaleAdult"),
        (SlaughterColumns.XYFC, SlaughterColumns.XYFM + SlaughterColumns.WCol - SlaughterColumns.XYFC, "AnimalFemaleYoung"),
        (SlaughterColumns.XPregC, SlaughterColumns.XPregT + SlaughterColumns.WToggle - SlaughterColumns.XPregC, "AnimalPregnant"),
        (SlaughterColumns.XBondC, SlaughterColumns.XBondT + SlaughterColumns.WToggle - SlaughterColumns.XBondC, "AnimalBonded"),
        (SlaughterColumns.XProtT, SlaughterColumns.WProt + SlaughterColumns.WProt, ASMKeys.Protection),
    };

    // (x, width, labelKey, tipKey).
    private static readonly (float X, float Width, string LabelKey, string TipKey)[] SubCells =
    {
        (SlaughterColumns.XName, SlaughterColumns.WName, "AutoSlaugtherHeaderColLabel", "AutoSlaugtherHeaderTooltipLabel"),
        (SlaughterColumns.XTotC, SlaughterColumns.WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaugtherHeaderTooltipCurrentTotal"),
        (SlaughterColumns.XTotM, SlaughterColumns.WCol, "AutoSlaugtherHeaderColMax", "AutoSlaugtherHeaderTooltipMaxTotal"),
        (SlaughterColumns.XAMC, SlaughterColumns.WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaugtherHeaderTooltipCurrentMales"),
        (SlaughterColumns.XAMM, SlaughterColumns.WCol, "AutoSlaugtherHeaderColMax", "AutoSlaugtherHeaderTooltipMaxMales"),
        (SlaughterColumns.XYMC, SlaughterColumns.WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaughterHeaderTooltipCurrentMalesYoung"),
        (SlaughterColumns.XYMM, SlaughterColumns.WCol, "AutoSlaugtherHeaderColMax", "AutoSlaughterHeaderTooltipMaxMalesYoung"),
        (SlaughterColumns.XAFC, SlaughterColumns.WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaugtherHeaderTooltipCurrentFemales"),
        (SlaughterColumns.XAFM, SlaughterColumns.WCol, "AutoSlaugtherHeaderColMax", "AutoSlaugtherHeaderTooltipMaxFemales"),
        (SlaughterColumns.XYFC, SlaughterColumns.WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaughterHeaderTooltipCurrentFemalesYoung"),
        (SlaughterColumns.XYFM, SlaughterColumns.WCol, "AutoSlaugtherHeaderColMax", "AutoSlaughterHeaderTooltipMaxFemalesYoung"),
        (SlaughterColumns.XPregC, SlaughterColumns.WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaughterHeaderTooltipCurrentPregnant"),
        (SlaughterColumns.XPregT, SlaughterColumns.WToggle, "AllowSlaughter", "AutoSlaughterHeaderTooltipAllowSlaughterPregnant"),
        (SlaughterColumns.XBondC, SlaughterColumns.WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaughterHeaderTooltipCurrentBonded"),
        (SlaughterColumns.XBondT, SlaughterColumns.WToggle, "AllowSlaughter", "AutoSlaughterHeaderTooltipAllowSlaughterBonded"),
        (SlaughterColumns.XProtT, SlaughterColumns.WProt, ASMKeys.ProtTraits, ASMKeys.TipTraits),
        (SlaughterColumns.XProtI, SlaughterColumns.WProt, ASMKeys.ProtIndivSub, ASMKeys.TipIndiv),
    };

    public static void Draw(Rect rect)
    {
        float ox = rect.x;
        float topY = rect.y;
        float subY = rect.y + SlaughterColumns.HeaderTopH;

        // Top-row lighter background for every group (name group is the exception:
        // its top has no bg, its "Вид" subheader has the bg).
        foreach (var g in Groups)
        {
            Widgets.DrawLightHighlight(new Rect(ox + g.X, topY, g.Width, SlaughterColumns.HeaderTopH));
        }

        Widgets.DrawLightHighlight(new Rect(ox + SlaughterColumns.XName, subY, SlaughterColumns.WName, SlaughterColumns.HeaderSubH));

        // Top-row titles.
        foreach (var g in Groups)
        {
            HLabel(ox + g.X, topY, g.Width, SlaughterColumns.HeaderTopH, g.TitleKey.Translate());
        }

        // Sub-row headers with hover highlight + tooltip.
        foreach (var s in SubCells)
        {
            Rect cell = new Rect(ox + s.X, subY, s.Width, SlaughterColumns.HeaderSubH);
            Widgets.DrawHighlightIfMouseover(cell);
            TooltipHandler.TipRegion(cell, s.TipKey.Translate());
            HLabel(ox + s.X, subY, s.Width, SlaughterColumns.HeaderSubH, s.LabelKey.Translate());
        }

        // Gray gridlines: vertical at each group's right edge (no line between icon and name),
        // and a horizontal line under the header.
        var color = GUI.color;
        GUI.color = Color.gray;

        foreach (var g in Groups)
        {
            Widgets.DrawLineVertical(ox + g.X + g.Width, rect.y, rect.height);
        }

        Widgets.DrawLineHorizontal(ox, rect.yMax, rect.width);
        GUI.color = color;
    }

    private static void HLabel(float x, float y, float w, float h, string s)
    {
        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(x, y, w, h), s);
        Text.Anchor = anchor;
    }
}
