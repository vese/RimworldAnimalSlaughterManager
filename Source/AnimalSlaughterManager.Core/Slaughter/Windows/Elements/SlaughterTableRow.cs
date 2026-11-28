using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>One animal-kind row: the icon, the indicator-marked name, the count/limit cells,
/// the pregnant-mode and paint toggles, the protection counts and the configure button.</summary>
internal static class SlaughterTableRow
{
    public static void Draw(Rect row, AutoSlaughterConfig config, ASM_MapComp comp, Map map, int index, ref int paintCol, ref bool paintValue)
    {
        // Zebra rows, like the preset browser's list.
        if (index % 2 == 1)
        {
            Widgets.DrawLightHighlight(row);
        }

        var def = config.animal;
        bool hasKs = comp.kindSettings.TryGetValue(def, out var ks);
        // Indicator categories. Individual protection is intentionally NOT indicated here
        // (it has its own column); only per-kind trait/priority settings are.
        bool hasProtection = hasKs && ServiceRegistry.GetServices<IKeepReserveSource>().Any(s => s.HasKeepRules(ks));
        bool hasForceCull = hasKs && ServiceRegistry.GetServices<IKindForceCullSource>().Any(s => s.HasForceCullRules(ks));
        bool hasPriority = hasKs && comp.KindHasCustomPrefs(def);
        SlaughterTable.CountKind(map, def, config, comp, out int total, out int m, out int my, out int f, out int fy, out int preg, out int bond);

        Widgets.ThingIcon(new Rect(row.x + SlaughterColumns.XIcon, row.y + 2f, SlaughterColumns.WIcon - 4f, SlaughterColumns.WIcon - 4f), def);

        // Exclusive precedence: protection > force-cull > priority (they do not stack).
        string prefix;
        Color nameColor;

        if (hasProtection)
        {
            prefix = "★ ";
            nameColor = Color.green;
        }
        else if (hasForceCull)
        {
            prefix = "▼ ";
            nameColor = Color.red;
        }
        else if (hasPriority)
        {
            prefix = "◆ ";
            nameColor = Color.cyan;
        }
        else
        {
            prefix = "";
            nameColor = total == 0 ? Color.gray : Color.white;
        }

        string fullName = prefix + def.LabelCap.ToString();
        Rect nameRect = new Rect(row.x + SlaughterColumns.XName, row.y, SlaughterColumns.WName, row.height);

        if (hasProtection || hasForceCull || hasPriority)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (hasPriority)
            {
                parts.Add(ASMKeys.IndicatorPriority.Translate());
            }

            if (hasForceCull)
            {
                parts.Add(ASMKeys.IndicatorForceCull.Translate());
            }

            if (hasProtection)
            {
                parts.Add(ASMKeys.IndicatorProtection.Translate());
            }

            TooltipHandler.TipRegion(nameRect, string.Join("\n", parts));
        }

        bool absent = total == 0;
        CurCol(row, SlaughterColumns.XTotC, total, config.maxTotal != -1 && total > config.maxTotal, absent);
        MaxCol(row, SlaughterColumns.XTotM, ref config.maxTotal, ref config.uiMaxTotalBuffer, total, map);
        CurCol(row, SlaughterColumns.XAMC, m, config.maxMales != -1 && m > config.maxMales, absent);
        MaxCol(row, SlaughterColumns.XAMM, ref config.maxMales, ref config.uiMaxMalesBuffer, m, map);
        CurCol(row, SlaughterColumns.XAFC, f, config.maxFemales != -1 && f > config.maxFemales, absent);
        MaxCol(row, SlaughterColumns.XAFM, ref config.maxFemales, ref config.uiMaxFemalesBuffer, f, map);
        CurCol(row, SlaughterColumns.XYMC, my, config.maxMalesYoung != -1 && my > config.maxMalesYoung, absent);
        MaxCol(row, SlaughterColumns.XYMM, ref config.maxMalesYoung, ref config.uiMaxMalesYoungBuffer, my, map);
        CurCol(row, SlaughterColumns.XYFC, fy, config.maxFemalesYoung != -1 && fy > config.maxFemalesYoung, absent);
        MaxCol(row, SlaughterColumns.XYFM, ref config.maxFemalesYoung, ref config.uiMaxFemalesYoungBuffer, fy, map);
        var pregMode = comp.ResolvePregnantMode(def, config.allowSlaughterPregnant);
        PregnantCountCol(row, SlaughterColumns.XPregC, preg, false, absent, def);
        PregnantModeButton(row, SlaughterColumns.XPregC, SlaughterColumns.XPregT + SlaughterColumns.WToggle - SlaughterColumns.XPregC, def, config, pregMode, comp, map);
        CurCol(row, SlaughterColumns.XBondC, bond, false, absent);
        PaintToggle(row, SlaughterColumns.XBondT, 1, ref config.allowSlaughterBonded, ref paintCol, ref paintValue, map);
        CountCol(row, SlaughterColumns.XProtT, SlaughterListBuilder.CountKeptByTraits(comp, def), absent);
        CountCol(row, SlaughterColumns.XProtI, SlaughterListBuilder.CountIndividuallyProtected(comp, def), absent);

        if (Widgets.ButtonText(new Rect(row.x + SlaughterColumns.XConfig, row.y + 2f, SlaughterColumns.WConfig, row.height - 4f), ASMKeys.ConfigureKind.Translate()))
        {
            Find.WindowStack.Add(new KindSlaughterSettingsDialog(comp, def));
        }

        // Name is drawn last so the full text (shown only on hover) can sit on top of the
        // count cells. Otherwise it's truncated with an ellipsis inside the Name column.
        var color = GUI.color;
        var anchor = Text.Anchor;
        var wrap = Text.WordWrap;
        GUI.color = nameColor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.WordWrap = false;

        if (Mouse.IsOver(nameRect))
        {
            Widgets.Label(new Rect(row.x + SlaughterColumns.XName, row.y, SlaughterColumns.XConfig - SlaughterColumns.XName, row.height), fullName);
        }
        else
        {
            Widgets.Label(nameRect, fullName.Truncate(nameRect.width));
        }

        Text.WordWrap = wrap;
        GUI.color = color;
        Text.Anchor = anchor;
    }

    private static void CurCol(Rect row, float x, int count, bool red, bool absent)
    {
        var color = GUI.color;

        if (red)
        {
            GUI.color = Color.red;
        }
        else if (count == 0 && absent)
        {
            GUI.color = Color.gray;
        }

        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(row.x + x, row.y, SlaughterColumns.WCol, row.height), count.ToString());
        Text.Anchor = anchor;
        GUI.color = color;
    }

    private static void CountCol(Rect row, float x, int count, bool absent)
    {
        var color = GUI.color;

        if (count == 0 && absent)
        {
            GUI.color = Color.gray;
        }

        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(row.x + x, row.y, SlaughterColumns.WProt, row.height), count.ToString());
        Text.Anchor = anchor;
        GUI.color = color;
    }

    // Pregnant/egg count cell. For egg-laying kinds, prefix the number with the kind's own egg
    // icon so it reads as "carrying eggs" rather than mammalian pregnancy.
    private static void PregnantCountCol(Rect row, float x, int count, bool red, bool absent, ThingDef def)
    {
        var eggProps = def.GetCompProperties<CompProperties_EggLayer>();
        ThingDef eggDef = eggProps == null ? null : (eggProps.eggFertilizedDef ?? eggProps.eggUnfertilizedDef);

        if (eggDef == null)
        {
            CurCol(row, x, count, red, absent);

            return;
        }

        Rect cell = new Rect(row.x + x, row.y, SlaughterColumns.WCol, row.height);
        // Count centered in the "Сейчас" cell; egg icon sits just left of the number.
        var color = GUI.color;

        if (red)
        {
            GUI.color = Color.red;
        }
        else if (count == 0 && absent)
        {
            GUI.color = Color.gray;
        }

        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(cell, count.ToString());
        Text.Anchor = anchor;
        GUI.color = color;
        float numW = Text.CalcSize(count.ToString()).x;
        Rect icon = new Rect(cell.center.x - numW / 2f - 2f - 16f, row.y + (row.height - 16f) / 2f, 16f, 16f);
        TooltipHandler.TipRegion(icon, ASMKeys.EggLayInfo.Translate(eggDef.LabelCap, eggProps.eggLayIntervalDays.ToString("0.##")));
        Widgets.ThingIcon(icon, eggDef);
    }

    private static void MaxCol(Rect row, float x, ref int val, ref string buffer, int current, Map map)
    {
        const float cw = SlaughterColumns.WCol;       // fill the whole 60px column: field(40)+gap(4)+X(16) fits 3 digits
        float cx0 = row.x + x;                         // centered => exactly the column

        if (val == -1)
        {
            Rect b = new Rect(cx0, row.y + (row.height - 26f) / 2f, cw, 26f);
            TooltipHandler.TipRegion(b, "AutoSlaughterTooltipSetLimit".Translate());

            if (Widgets.ButtonImageWithBG(b, TexButton.Infinity, new Vector2(24f, 24f)))
            {
                val = current;
                buffer = null;
                NotifyChanged(map);
            }
        }
        else
        {
            const float fieldW = 40f;    // field(40) + gap(4) + X(16) = 60 = cw
            Rect field = new Rect(cx0, row.y + (row.height - 22f) / 2f, fieldW, 22f);
            string s = Widgets.TextField(field, buffer ?? val.ToString());
            buffer = s;

            if (int.TryParse(s, out int v) && v != val)
            {
                val = Mathf.Max(0, v);
                NotifyChanged(map);
            }

            Rect xBtn = new Rect(cx0 + fieldW + 4f, row.y + (row.height - 16f) / 2f, 16f, 16f);

            if (Widgets.ButtonImage(xBtn, TexButton.CloseXSmall))
            {
                val = -1;
                buffer = null;
                NotifyChanged(map);
            }
        }
    }

    // Click-and-drag (paint) toggle for the Pregnant/Bonded columns: press one row's checkbox
    // and drag down across rows to set the same state on every row in that column. Each column
    // paints independently (paintCol), and the checkbox is draw-only to avoid a double-toggle.
    private static void PaintToggle(Rect row, float x, int colId, ref bool value, ref int paintCol, ref bool paintValue, Map map)
    {
        Rect cb = new Rect(row.x + x + (SlaughterColumns.WToggle - 24f) / 2f, row.y + (row.height - 24f) / 2f, 24f, 24f);

        if (Mouse.IsOver(cb))
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                paintCol = colId;
                paintValue = !value;
                value = paintValue;
                NotifyChanged(map);
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDrag && paintCol == colId)
            {
                if (value != paintValue)
                {
                    value = paintValue;
                    NotifyChanged(map);
                }
            }
        }

        Widgets.CheckboxDraw(cb.x, cb.y, value, false, 24f);

        if (Event.current.type == EventType.MouseUp)
        {
            paintCol = -1;
        }
    }

    // Three-state pregnant toggle centered in the whole "Беременные" column. All three states
    // use the same white 24px treatment (shape distinguishes them): X = Never, pause = Defer,
    // check = Always.
    private static void PregnantModeButton(Rect row, float groupX, float groupW, ThingDef def, AutoSlaughterConfig config, PregnantMode mode, ASM_MapComp comp, Map map)
    {
        const float iconS = 24f;
        Rect b = new Rect(row.x + groupX + (groupW - iconS) / 2f, row.y + (row.height - iconS) / 2f, iconS, iconS);
        TooltipHandler.TipRegion(b, PregnantModeTip(mode));

        if (mode == PregnantMode.Always)
        {
            Widgets.CheckboxDraw(b.x, b.y, true, false, iconS);
        }
        else if (mode == PregnantMode.Defer)
        {
            GUI.DrawTexture(b, TexButton.Suspend);
        }
        else
        {
            Widgets.CheckboxDraw(b.x, b.y, false, false, iconS);
        }

        if (Widgets.ButtonInvisible(b))
        {
            var next = mode == PregnantMode.Never ? PregnantMode.Defer : mode == PregnantMode.Defer ? PregnantMode.Always : PregnantMode.Never;
            comp.pregnantModes[def] = next;
            config.allowSlaughterPregnant = next == PregnantMode.Always;
            NotifyChanged(map);
        }
    }

    private static string PregnantModeTip(PregnantMode mode) => mode switch
    {
        PregnantMode.Defer => ASMKeys.PregDefer.Translate(),
        PregnantMode.Always => ASMKeys.PregAlways.Translate(),
        _ => ASMKeys.PregNever.Translate(),
    };

    private static void NotifyChanged(Map map) => map?.autoSlaughterManager?.Notify_ConfigChanged();
}
