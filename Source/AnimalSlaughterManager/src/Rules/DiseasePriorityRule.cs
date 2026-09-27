using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ASM;

public class DiseasePriorityRule : BasePriorityRule
{
    public bool has = true;
    public HediffDef? disease;

    public override bool HasNullDef => disease == null;

    public override string Label => (has ? ASMKeys.CondHas : ASMKeys.CondMissing).Translate(DiseaseLabel(disease));

    public override BasePriorityRule Clone() => new DiseasePriorityRule() { has = has, disease = disease };

    public override bool Matches(Pawn? p) => disease != null && (p?.health?.hediffSet?.HasHediff(disease) ?? false) == has;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref has, "has", true);
        Scribe_Defs.Look(ref disease, "disease");
    }

    public override IEnumerable<Type> TraitSetTypes
    {
        get
        {
            yield return typeof(HealthTraitSet);
        }
    }

    public override IEnumerable<IRuleSetValidator> Validators => [new RedundantRuleValidator(), new DuplicateValidator()];

    public override bool Covers(BasePriorityRule other) =>
        (other is DiseasePriorityRule rule && has == rule.has && disease?.defName == rule.disease?.defName) ||
        // Healthy ⊆ not having a specific disease.
        (other is DiseaseAnyPriorityRule any && !has && !any.has);

    protected override void ChangeVariantInternal() => has = !has;

    // If multiple disease defs share the same label, append the defName for disambiguation.
    private static string DiseaseLabel(HediffDef? disease)
    {
        if (disease == null)
        {
            return Constants.MissingLabel;
        }

        var label = disease.LabelCap.ToString();
        bool dup = false;

        foreach (var other in DefDatabase<HediffDef>.AllDefs)
        {
            if (other != disease && other.makesSickThought && other.LabelCap.ToString() == label)
            {
                dup = true;
                break;
            }
        }
        return dup ? $"{label} ({disease.defName})" : label;
    }
}
