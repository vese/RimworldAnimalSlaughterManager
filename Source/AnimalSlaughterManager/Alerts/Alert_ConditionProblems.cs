using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// Persistent alert (top-right, red) shown when any kind has duplicate or contradictory
/// conditions in its per-bucket priority lists. Clicking opens the first problem kind's
/// settings window.
/// </summary>
public class Alert_ConditionProblems : Alert
{
    public Alert_ConditionProblems()
    {
        defaultPriority = AlertPriority.Critical;
    }

    private readonly List<KeyValuePair<ThingDef, int>> problemKinds = [];

    public override AlertReport GetReport()
    {
        if (Find.CurrentMap == null)
        {
            return AlertReport.Inactive;
        }

        var comp = Find.CurrentMap.GetComponent<ASM_MapComp>();

        if (comp == null || comp.kindSettings == null || comp.kindSettings.Count == 0)
        {
            return AlertReport.Inactive;
        }

        problemKinds.Clear();

        foreach (var kv in comp.kindSettings)
        {
            if (kv.Value == null)
            {
                continue;
            }

            var issues = CountProblems(kv.Value);

            if (issues > 0)
            {
                problemKinds.Add(new KeyValuePair<ThingDef, int>(kv.Key, issues));
            }
        }

        return problemKinds.Count > 0 ? AlertReport.Active : AlertReport.Inactive;
    }

    public override string GetLabel() => ASMKeys.AlertConditionLabel.Translate();

    public override TaggedString GetExplanation()
    {
        var sb = new StringBuilder();
        sb.AppendLine(ASMKeys.AlertConditionDesc.Translate());
        foreach (var pk in problemKinds)
        {
            sb.AppendLine("  " + pk.Key.LabelCap + " (" + pk.Value + ")");
        }
        return sb.ToString();
    }

    protected override void OnClick()
    {
        var comp = Find.CurrentMap?.GetComponent<ASM_MapComp>();

        if (comp == null)
        {
            return;
        }

        foreach (var kv in comp.kindSettings)
        {
            if (kv.Value == null)
            {
                continue;
            }

            if (CountProblems(kv.Value) > 0)
            {
                Find.WindowStack.Add(new Dialog_KindSlaughterSettings(comp, kv.Key));
                return;
            }
        }
    }

    // TODO: do method in KindPrioritySettings
    private static int CountProblems(KindSettings ks) => ks.prioritySettings.ruleSets.Keys
        .Select(key => ks.prioritySettings.Validate(key.Male, key.Adult))
        .Sum(x => x.Count(errors => errors is not null && errors.Count > 0));
}
