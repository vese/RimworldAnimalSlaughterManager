using System.Collections.Generic;
using System.Linq;

using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>The table itself: the configured kinds (present animals first) inside a scroll
/// view, the header on top and the zebra rows below.</summary>
internal static class SlaughterTable
{
    public static void Draw(Rect outRect, ASM_MapComp comp, Map map, ref Vector2 scroll, ref int paintCol, ref bool paintValue)
    {
        var configs = map?.autoSlaughterManager?.configs;

        if (configs == null)
        {
            return;
        }

        var sorted = configs
            .Where(c => c?.animal != null)
            .OrderBy(c => CountTotal(map!, c.animal) > 0 ? 0 : 1)
            .ThenBy(c => c.animal.LabelCap.ToString())
            .ToList();

        float contentH = SlaughterColumns.HeaderH + 2f + sorted.Count * (SlaughterColumns.RowH + 2f);
        Rect view = new Rect(outRect.x, outRect.y, SlaughterColumns.TableWidth, Mathf.Max(contentH, outRect.height));
        Widgets.BeginScrollView(outRect, ref scroll, view);

        SlaughterTableHeader.Draw(new Rect(view.x, view.y, view.width, SlaughterColumns.HeaderH));

        float cy = view.y + SlaughterColumns.HeaderH + 2f;
        int index = 0;

        foreach (var config in sorted)
        {
            Rect row = new Rect(view.x, cy, view.width, SlaughterColumns.RowH);
            SlaughterTableRow.Draw(row, config, comp, map!, index, ref paintCol, ref paintValue);
            cy += SlaughterColumns.RowH + 2f;
            index++;
        }

        Widgets.EndScrollView();
    }

    public static int CountTotal(Map map, ThingDef def)
    {
        int t = 0;

        foreach (var pa in map.mapPawns.SpawnedColonyAnimals)
        {
            if (pa.def == def)
            {
                t++;
            }
        }

        return t;
    }

    public static void CountKind(Map map, ThingDef def, AutoSlaughterConfig config, ASM_MapComp comp, out int total, out int m, out int my, out int f, out int fy, out int preg, out int bond)
    {
        total = m = my = f = fy = preg = bond = 0;
        // The total and sex×age counts reflect only animals that can be slaughtered: when the
        // pregnant/bonded toggle is off, those animals are not counted toward them.
        bool countPregnant = comp.ResolvePregnantMode(def, config.allowSlaughterPregnant) != PregnantMode.Never;
        bool countBonded = config.allowSlaughterBonded;

        foreach (var pa in map.mapPawns.SpawnedColonyAnimals)
        {
            if (pa.def != def)
            {
                continue;
            }

            bool isPregnant = pa.gender == Gender.Female && PregnancyUtility.IsPregnantOrCarryingEgg(pa);
            bool isBonded = pa.relations.GetDirectRelationsCount(PawnRelationDefOf.Bond) > 0;

            if ((countPregnant || !isPregnant) && (countBonded || !isBonded))
            {
                total++;
                bool repro = pa.ageTracker.CurLifeStage.reproductive;

                if (pa.gender == Gender.Male) { if (repro) m++; else my++; }
                else if (pa.gender == Gender.Female) { if (repro) f++; else fy++; }
            }

            if (isPregnant)
            {
                preg++;
            }

            if (isBonded)
            {
                bond++;
            }
        }
    }
}
