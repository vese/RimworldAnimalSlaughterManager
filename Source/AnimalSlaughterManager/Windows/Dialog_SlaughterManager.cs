using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM
{
    /// <summary>
    /// Global Animal Slaughter Manager as a table that mirrors the vanilla slaughter-management
    /// window's look and behavior (lighter header background, gray gridlines, hover tooltips with
    /// vanilla text, ∞/clear limit buttons). Adds Pregnant/Bonded "Забить" toggles, a "Защита"
    /// group (by traits / individually), and a Configure button.
    /// </summary>
    public class Dialog_SlaughterManager : Window
    {
        private readonly ASM_MapComp comp;
        private readonly Map map;
        private Vector2 scroll;
        private int paintCol = -1;   // active paint column for Pregnant/Bonded toggles (-1 = none)
        private bool paintValue;

        private const float RowH = 30f;
        private const float HeaderTopH = 22f;
        private const float HeaderSubH = 22f;
        private const float HeaderH = HeaderTopH + HeaderSubH;

        // Column geometry. Wider icon column; "Всего" is the same width as the sex×age columns.
        private const float XIcon = 0f, WIcon = 32f;
        private const float XName = 32f, WName = 110f;            // name group (no top bg, no top title)
        private const float XTotC = 142f, XTotM = 202f, WCol = 60f;   // Всего 142..262
        private const float XAMC = 262f, XAMM = 322f;             // Взрослые самцы 262..382
        private const float XYMC = 382f, XYMM = 442f;             // Молодые самцы 382..502
        private const float XAFC = 502f, XAFM = 562f;             // Взрослые самки 502..622
        private const float XYFC = 622f, XYFM = 682f;             // Молодые самки 622..742
        private const float XPregC = 742f, XPregT = 802f, WToggle = 72f;   // Беременные 742..874
        private const float XBondC = 874f, XBondT = 934f;         // С привязанностью 874..1006
        private const float XProtT = 1006f, XProtI = 1086f, WProt = 80f;   // Защита 1006..1166
        private const float XConfig = 1166f, WConfig = 100f;
        private const float TableWidth = 1266f;

        public override Vector2 InitialSize => new Vector2(TableWidth + 64f, 680f);

        public Dialog_SlaughterManager(ASM_MapComp comp)
        {
            this.comp = comp;
            this.map = Find.CurrentMap;
            doCloseX = true;
            draggable = true;
            resizeable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Rect outRect = new Rect(inRect.x, inRect.y + 12f, inRect.width, inRect.height - 52f);
            DrawTable(outRect);

            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - 32f, inRect.width, 30f), ASMKeys.ManagePresets.Translate()))
                Find.WindowStack.Add(new Dialog_PresetBrowser(comp, PresetScope.All, null, null));
        }

        private void DrawTable(Rect outRect)
        {
            var configs = map?.autoSlaughterManager?.configs;
            if (configs == null) return;

            var sorted = configs
                .Where(c => c?.animal != null)
                .OrderBy(c => CountTotal(c.animal) > 0 ? 0 : 1)
                .ThenBy(c => c.animal.LabelCap.ToString())
                .ToList();

            float contentH = HeaderH + 2f + sorted.Count * (RowH + 2f);
            Rect view = new Rect(outRect.x, outRect.y, TableWidth, Mathf.Max(contentH, outRect.height));
            Widgets.BeginScrollView(outRect, ref scroll, view);

            DrawHeader(new Rect(view.x, view.y, view.width, HeaderH));

            float cy = view.y + HeaderH + 2f;
            for (int i = 0; i < sorted.Count; i++)
            {
                var config = sorted[i];
                Rect row = new Rect(view.x, cy, view.width, RowH);
                if (i % 2 == 1) Widgets.DrawLightHighlight(row);
                DrawRow(row, config);
                cy += RowH + 2f;
            }
            Widgets.EndScrollView();
        }

        // (startX, width, topTitleKey). Name group has no top title.
        private static readonly (float, float, string)[] Groups =
        {
            (XTotC, XTotM + WCol - XTotC, "AutoSlaugtherHeaderColTotal"),
            (XAMC, XAMM + WCol - XAMC, "AnimalMaleAdult"),
            (XYMC, XYMM + WCol - XYMC, "AnimalMaleYoung"),
            (XAFC, XAFM + WCol - XAFC, "AnimalFemaleAdult"),
            (XYFC, XYFM + WCol - XYFC, "AnimalFemaleYoung"),
            (XPregC, XPregT + WToggle - XPregC, "AnimalPregnant"),
            (XBondC, XBondT + WToggle - XBondC, "AnimalBonded"),
            (XProtT, WProt + WProt, ASMKeys.Protection),
        };

        // (x, width, labelKey, tipKey).
        private static readonly (float, float, string, string)[] SubCells =
        {
            (XName, WName, "AutoSlaugtherHeaderColLabel", "AutoSlaugtherHeaderTooltipLabel"),
            (XTotC, WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaugtherHeaderTooltipCurrentTotal"),
            (XTotM, WCol, "AutoSlaugtherHeaderColMax", "AutoSlaugtherHeaderTooltipMaxTotal"),
            (XAMC, WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaugtherHeaderTooltipCurrentMales"),
            (XAMM, WCol, "AutoSlaugtherHeaderColMax", "AutoSlaugtherHeaderTooltipMaxMales"),
            (XYMC, WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaughterHeaderTooltipCurrentMalesYoung"),
            (XYMM, WCol, "AutoSlaugtherHeaderColMax", "AutoSlaughterHeaderTooltipMaxMalesYoung"),
            (XAFC, WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaugtherHeaderTooltipCurrentFemales"),
            (XAFM, WCol, "AutoSlaugtherHeaderColMax", "AutoSlaugtherHeaderTooltipMaxFemales"),
            (XYFC, WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaugtherHeaderTooltipCurrentFemalesYoung"),
            (XYFM, WCol, "AutoSlaugtherHeaderColMax", "AutoSlaughterHeaderTooltipMaxFemalesYoung"),
            (XPregC, WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaughterHeaderTooltipCurrentPregnant"),
            (XPregT, WToggle, "AllowSlaughter", "AutoSlaughterHeaderTooltipAllowSlaughterPregnant"),
            (XBondC, WCol, "AutoSlaugtherHeaderColCurrent", "AutoSlaughterHeaderTooltipCurrentBonded"),
            (XBondT, WToggle, "AllowSlaughter", "AutoSlaughterHeaderTooltipAllowSlaughterBonded"),
            (XProtT, WProt, ASMKeys.ProtTraits, ASMKeys.TipTraits),
            (XProtI, WProt, ASMKeys.ProtIndivSub, ASMKeys.TipIndiv),
        };

        private void DrawHeader(Rect r)
        {
            float ox = r.x;
            float topY = r.y;
            float subY = r.y + HeaderTopH;

            // Top-row lighter background for every group (name group is the exception:
            // its top has no bg, its "Вид" subheader has the bg).
            foreach (var g in Groups)
                Widgets.DrawLightHighlight(new Rect(ox + g.Item1, topY, g.Item2, HeaderTopH));
            Widgets.DrawLightHighlight(new Rect(ox + XName, subY, WName, HeaderSubH));

            // Top-row titles.
            foreach (var g in Groups)
                HLabel(ox + g.Item1, topY, g.Item2, HeaderTopH, g.Item3.Translate());

            // Sub-row headers with hover highlight + tooltip.
            foreach (var s in SubCells)
            {
                Rect cell = new Rect(ox + s.Item1, subY, s.Item2, HeaderSubH);
                Widgets.DrawHighlightIfMouseover(cell);
                TooltipHandler.TipRegion(cell, s.Item4.Translate());
                HLabel(ox + s.Item1, subY, s.Item2, HeaderSubH, s.Item3.Translate());
            }

            // Gray gridlines: vertical at each group's right edge (no line between icon and name),
            // and a horizontal line under the header.
            GUI.color = Color.gray;
            foreach (var g in Groups)
                Widgets.DrawLineVertical(ox + g.Item1 + g.Item2, r.y, r.height);
            Widgets.DrawLineHorizontal(ox, r.yMax, r.width);
            GUI.color = Color.white;
        }

        private void DrawRow(Rect row, AutoSlaughterConfig config)
        {
            var def = config.animal;
            bool hasKs = comp.kindSettings.TryGetValue(def, out var ks) && ks != null;
            // Indicator categories. Individual protection is intentionally NOT indicated here
            // (it has its own column); only per-kind trait/priority settings are.
            bool hasProtection = hasKs && ks.HasProtectionSettings;
            bool hasForceCull = hasKs && ks.HasForceCullSettings;
            bool hasPriority = hasKs && comp.KindHasCustomPrefs(def);
            CountKind(def, config, out int total, out int m, out int my, out int f, out int fy, out int preg, out int bond);

            Widgets.ThingIcon(new Rect(row.x + XIcon, row.y + 2f, WIcon - 4f, WIcon - 4f), def);

            // Exclusive precedence: protection > force-cull > priority (they do not stack).
            string prefix;
            Color nameColor;
            if (hasProtection) { prefix = "★ "; nameColor = Color.green; }
            else if (hasForceCull) { prefix = "▼ "; nameColor = Color.red; }
            else if (hasPriority) { prefix = "◆ "; nameColor = Color.cyan; }
            else { prefix = ""; nameColor = total == 0 ? Color.gray : Color.white; }
            string fullName = prefix + def.LabelCap.ToString();
            Rect nameRect = new Rect(row.x + XName, row.y, WName, row.height);
            if (hasProtection || hasForceCull || hasPriority)
            {
                var parts = new List<string>();
                if (hasPriority) parts.Add(ASMKeys.IndicatorPriority.Translate());
                if (hasForceCull) parts.Add(ASMKeys.IndicatorForceCull.Translate());
                if (hasProtection) parts.Add(ASMKeys.IndicatorProtection.Translate());
                TooltipHandler.TipRegion(nameRect, string.Join("\n", parts));
            }

            bool absent = total == 0;
            CurCol(row, XTotC, total, config.maxTotal != -1 && total > config.maxTotal, absent);
            MaxCol(row, XTotM, ref config.maxTotal, ref config.uiMaxTotalBuffer, total);
            CurCol(row, XAMC, m, config.maxMales != -1 && m > config.maxMales, absent);
            MaxCol(row, XAMM, ref config.maxMales, ref config.uiMaxMalesBuffer, m);
            CurCol(row, XAFC, f, config.maxFemales != -1 && f > config.maxFemales, absent);
            MaxCol(row, XAFM, ref config.maxFemales, ref config.uiMaxFemalesBuffer, f);
            CurCol(row, XYMC, my, config.maxMalesYoung != -1 && my > config.maxMalesYoung, absent);
            MaxCol(row, XYMM, ref config.maxMalesYoung, ref config.uiMaxMalesYoungBuffer, my);
            CurCol(row, XYFC, fy, config.maxFemalesYoung != -1 && fy > config.maxFemalesYoung, absent);
            MaxCol(row, XYFM, ref config.maxFemalesYoung, ref config.uiMaxFemalesYoungBuffer, fy);
            var pregMode = comp.ResolvePregnantMode(def, config.allowSlaughterPregnant);
            PregnantCountCol(row, XPregC, preg, false, absent, def);
            PregnantModeButton(row, XPregC, XPregT + WToggle - XPregC, def, config, pregMode);
            CurCol(row, XBondC, bond, false, absent);
            PaintToggle(row, XBondT, 1, ref config.allowSlaughterBonded);
            CountCol(row, XProtT, comp.CountKeptByTraits(def), absent);
            CountCol(row, XProtI, comp.CountIndividuallyProtected(def), absent);

            if (Widgets.ButtonText(new Rect(row.x + XConfig, row.y + 2f, WConfig, row.height - 4f), ASMKeys.ConfigureKind.Translate()))
                Find.WindowStack.Add(new Dialog_KindSlaughterSettings(comp, def));

            // Name is drawn last so the full text (shown only on hover) can sit on top of the
            // count cells. Otherwise it's truncated with an ellipsis inside the Name column.
            GUI.color = nameColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            bool wrap = Text.WordWrap;
            Text.WordWrap = false;
            if (Mouse.IsOver(nameRect))
                Widgets.Label(new Rect(row.x + XName, row.y, XConfig - XName, row.height), fullName);
            else
                Widgets.Label(nameRect, fullName.Truncate(nameRect.width));
            Text.WordWrap = wrap;
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void HLabel(float x, float y, float w, float h, string s)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(x, y, w, h), s);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void CurCol(Rect row, float x, int count, bool red, bool absent)
        {
            if (red) GUI.color = Color.red;
            else if (count == 0 && absent) GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(row.x + x, row.y, WCol, row.height), count.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private static void CountCol(Rect row, float x, int count, bool absent)
        {
            if (count == 0 && absent) GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(row.x + x, row.y, WProt, row.height), count.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        // Pregnant/egg count cell. For egg-laying kinds, prefix the number with the kind's own egg
        // icon so it reads as "carrying eggs" rather than mammalian pregnancy.
        private static void PregnantCountCol(Rect row, float x, int count, bool red, bool absent, ThingDef def)
        {
            var eggProps = def.GetCompProperties<CompProperties_EggLayer>();
            ThingDef eggDef = eggProps == null ? null : (eggProps.eggFertilizedDef ?? eggProps.eggUnfertilizedDef);
            if (eggDef == null) { CurCol(row, x, count, red, absent); return; }
            Rect cell = new Rect(row.x + x, row.y, WCol, row.height);
            // Count centered in the "Сейчас" cell; egg icon sits just left of the number.
            if (red) GUI.color = Color.red;
            else if (count == 0 && absent) GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(cell, count.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            float numW = Text.CalcSize(count.ToString()).x;
            Rect icon = new Rect(cell.center.x - numW / 2f - 2f - 16f, row.y + (row.height - 16f) / 2f, 16f, 16f);
            TooltipHandler.TipRegion(icon, ASMKeys.EggLayInfo.Translate(eggDef.LabelCap, eggProps.eggLayIntervalDays.ToString("0.##")));
            Widgets.ThingIcon(icon, eggDef);
        }

        private void MaxCol(Rect row, float x, ref int val, ref string buffer, int current)
        {
            const float cw = WCol;           // fill the whole 60px column: field(40)+gap(4)+X(16) fits 3 digits
            float cx0 = row.x + x;           // centered => exactly the column
            if (val == -1)
            {
                Rect b = new Rect(cx0, row.y + (row.height - 26f) / 2f, cw, 26f);
                TooltipHandler.TipRegion(b, "AutoSlaughterTooltipSetLimit".Translate());
                if (Widgets.ButtonImageWithBG(b, TexButton.Infinity, new Vector2(24f, 24f)))
                { val = current; buffer = null; NotifyChanged(); }
            }
            else
            {
                const float fieldW = 40f;    // field(40) + gap(4) + X(16) = 60 = cw
                Rect field = new Rect(cx0, row.y + (row.height - 22f) / 2f, fieldW, 22f);
                string s = Widgets.TextField(field, buffer ?? val.ToString());
                buffer = s;
                if (int.TryParse(s, out int v) && v != val) { val = Mathf.Max(0, v); NotifyChanged(); }
                Rect xBtn = new Rect(cx0 + fieldW + 4f, row.y + (row.height - 16f) / 2f, 16f, 16f);
                if (Widgets.ButtonImage(xBtn, TexButton.CloseXSmall)) { val = -1; buffer = null; NotifyChanged(); }
            }
        }

        // Click-and-drag (paint) toggle for the Pregnant/Bonded columns: press one row's checkbox
        // and drag down across rows to set the same state on every row in that column. Each column
        // paints independently (paintCol), and the checkbox is draw-only to avoid a double-toggle.
        private void PaintToggle(Rect row, float x, int colId, ref bool value)
        {
            Rect cb = new Rect(row.x + x + (WToggle - 24f) / 2f, row.y + (row.height - 24f) / 2f, 24f, 24f);
            if (Mouse.IsOver(cb))
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    paintCol = colId;
                    paintValue = !value;
                    value = paintValue;
                    NotifyChanged();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDrag && paintCol == colId)
                {
                    if (value != paintValue) { value = paintValue; NotifyChanged(); }
                }
            }
            Widgets.CheckboxDraw(cb.x, cb.y, value, false, 24f);
            if (Event.current.type == EventType.MouseUp) paintCol = -1;
        }

        private void NotifyChanged() => map?.autoSlaughterManager?.Notify_ConfigChanged();

        // Three-state pregnant toggle centered in the whole "Беременные" column. All three states
        // use the same white 24px treatment (shape distinguishes them): X = Never, pause = Defer,
        // check = Always.
        private void PregnantModeButton(Rect row, float groupX, float groupW, ThingDef def, AutoSlaughterConfig config, PregnantMode mode)
        {
            float iconS = 24f;
            Rect b = new Rect(row.x + groupX + (groupW - iconS) / 2f, row.y + (row.height - iconS) / 2f, iconS, iconS);
            TooltipHandler.TipRegion(b, PregnantModeTip(mode));
            if (mode == PregnantMode.Always)
                Widgets.CheckboxDraw(b.x, b.y, true, false, iconS);
            else if (mode == PregnantMode.Defer)
                GUI.DrawTexture(b, TexButton.Suspend);
            else
                Widgets.CheckboxDraw(b.x, b.y, false, false, iconS);
            if (Widgets.ButtonInvisible(b))
            {
                var next = mode == PregnantMode.Never ? PregnantMode.Defer : mode == PregnantMode.Defer ? PregnantMode.Always : PregnantMode.Never;
                comp.pregnantModes[def] = next;
                config.allowSlaughterPregnant = next == PregnantMode.Always;
                NotifyChanged();
            }
        }

        private static string PregnantModeTip(PregnantMode mode)
        {
            switch (mode)
            {
                case PregnantMode.Defer: return ASMKeys.PregDefer.Translate();
                case PregnantMode.Always: return ASMKeys.PregAlways.Translate();
                default: return ASMKeys.PregNever.Translate();
            }
        }

        private int CountTotal(ThingDef def)
        {
            int t = 0;
            foreach (var pa in map.mapPawns.SpawnedColonyAnimals)
                if (pa.def == def) t++;
            return t;
        }

        private void CountKind(ThingDef def, AutoSlaughterConfig config, out int total, out int m, out int my, out int f, out int fy, out int preg, out int bond)
        {
            total = m = my = f = fy = preg = bond = 0;
            // The total and sex×age counts reflect only animals that can be slaughtered: when the
            // pregnant/bonded toggle is off, those animals are not counted toward them.
            bool countPregnant = comp.ResolvePregnantMode(def, config.allowSlaughterPregnant) != PregnantMode.Never;
            bool countBonded = config.allowSlaughterBonded;
            foreach (var pa in map.mapPawns.SpawnedColonyAnimals)
            {
                if (pa.def != def) continue;
                bool isPregnant = pa.gender == Gender.Female && ASM_MapComp.IsPregnantOrCarryingEgg(pa);
                bool isBonded = pa.relations.GetDirectRelationsCount(PawnRelationDefOf.Bond) > 0;
                if ((countPregnant || !isPregnant) && (countBonded || !isBonded))
                {
                    total++;
                    bool repro = pa.ageTracker.CurLifeStage.reproductive;
                    if (pa.gender == Gender.Male) { if (repro) m++; else my++; }
                    else if (pa.gender == Gender.Female) { if (repro) f++; else fy++; }
                }
                if (isPregnant) preg++;
                if (isBonded) bond++;
            }
        }
    }
}
