using System.Collections.Generic;
using ASM.Comps;
using RimWorld;
using Verse;

namespace ASM.Data
{
    public enum CondType
    {
        Pregnancy, Bond, Trait, DiseaseAny, Disease,
        Training, TrainingNone, TrainingPartial, TrainingFull,
        HasPositiveTrait, HasNegativeTrait
    }

    /// <summary>
    /// One element of a per-bucket slaughter-priority list. Position encodes keep/cull priority
    /// (top = keep, bottom = cull).
    /// </summary>
    public class SlaughterCondition : IExposable
    {
        public CondType type;
        public bool has = true;
        public HediffDef trait;
        public HediffDef disease;
        public TrainableDef trainable;
        public InheritableFilter inheritMode = InheritableFilter.Both;

        public SlaughterCondition() { }
        public SlaughterCondition(CondType type) { this.type = type; }

        public SlaughterCondition Clone() => new SlaughterCondition(type)
        {
            has = has,
            trait = trait,
            disease = disease,
            trainable = trainable,
            inheritMode = inheritMode,
        };

        /// <summary>True when this condition references a def (trait/disease/trainable) that failed
        /// to resolve on load (def-providing mod disabled). Such entries are no-ops and get pruned.</summary>
        public bool HasNullDef =>
            (type == CondType.Trait && trait == null) ||
            (type == CondType.Disease && disease == null) ||
            (type == CondType.Training && trainable == null);

        public bool Matches(Pawn p)
        {
            if (p == null) return false;
            switch (type)
            {
                case CondType.Pregnancy:
                    return ASM_MapComp.IsPregnantOrCarryingEgg(p) == has;
                case CondType.Bond:
                    return ((p.relations?.GetDirectRelationsCount(PawnRelationDefOf.Bond) ?? 0) > 0) == has;
                case CondType.Trait:
                    return trait != null && AnimalTraitsAccess.HasTrait(p, trait) == has && InheritMatch(trait, inheritMode);
                case CondType.DiseaseAny:
                    return IsSick(p) == has;
                case CondType.Disease:
                    return disease != null && (p.health?.hediffSet?.HasHediff(disease) ?? false) == has;
                case CondType.Training:
                    return trainable != null && (p.training?.HasLearned(trainable) ?? false) == has;
                case CondType.TrainingNone:
                    return !HasAnyTraining(p);
                case CondType.TrainingPartial:
                    return HasAnyTraining(p) && !AllTrained(p);
                case CondType.TrainingFull:
                    return AllTrained(p);
                case CondType.HasPositiveTrait:
                    return HasGoodBadTrait(p, false) == has;
                case CondType.HasNegativeTrait:
                    return HasGoodBadTrait(p, true) == has;
            }
            return false;
        }

        public string Label
        {
            get
            {
                switch (type)
                {
                    case CondType.Pregnancy: return has ? ASMKeys.CondPregnantHas.Translate() : ASMKeys.CondPregnantMissing.Translate();
                    case CondType.Bond: return has ? ASMKeys.CondBondHas.Translate() : ASMKeys.CondBondMissing.Translate();
                    case CondType.DiseaseAny: return has ? ASMKeys.CondDiseaseAnyHas.Translate() : ASMKeys.CondDiseaseAnyMissing.Translate();
                    case CondType.Trait: return (has ? ASMKeys.CondHas : ASMKeys.CondMissing).Translate(DefName(trait));
                    case CondType.Disease: return (has ? ASMKeys.CondHas : ASMKeys.CondMissing).Translate(DiseaseLabel(disease));
                    case CondType.Training: return (has ? ASMKeys.CondTrainingLearned : ASMKeys.CondTrainingNot).Translate(DefName(trainable));
                    case CondType.TrainingNone: return ASMKeys.CondTrainingNone.Translate();
                    case CondType.TrainingPartial: return ASMKeys.CondTrainingPartial.Translate();
                    case CondType.TrainingFull: return ASMKeys.CondTrainingFull.Translate();
                    case CondType.HasPositiveTrait: return has ? ASMKeys.CondPositiveHas.Translate() : ASMKeys.CondPositiveMissing.Translate();
                    case CondType.HasNegativeTrait: return has ? ASMKeys.CondNegativeHas.Translate() : ASMKeys.CondNegativeMissing.Translate();
                }
                return type.ToString();
            }
        }

        private static string DefName(Def d) => d == null ? "?" : d.LabelCap.ToString();

        private static bool InheritMatch(HediffDef trait, InheritableFilter mode)
        {
            switch (mode)
            {
                case InheritableFilter.Inheritable: return AnimalTraitsAccess.IsInheritable(trait);
                case InheritableFilter.NonInheritable: return !AnimalTraitsAccess.IsInheritable(trait);
                default: return true;
            }
        }

        // If multiple disease defs share the same label, append the defName for disambiguation.
        private static string DiseaseLabel(HediffDef d)
        {
            if (d == null) return "?";
            string label = d.LabelCap.ToString();
            bool dup = false;
            foreach (var other in DefDatabase<HediffDef>.AllDefs)
            {
                if (other != d && other.makesSickThought && other.LabelCap.ToString() == label)
                {
                    dup = true;
                    break;
                }
            }
            return dup ? label + " (" + d.defName + ")" : label;
        }

        private static bool IsSick(Pawn p)
        {
            var hediffs = p.health?.hediffSet?.hediffs;
            if (hediffs == null) return false;
            for (int i = 0; i < hediffs.Count; i++)
            {
                var h = hediffs[i];
                if (h?.def != null && h.def.makesSickThought) return true;
            }
            return false;
        }

        private static bool HasAnyTraining(Pawn p)
        {
            var tracker = p.training;
            if (tracker == null) return false;
            foreach (var td in TrainableUtility.TrainableDefsInListOrder)
                if (tracker.HasLearned(td)) return true;
            return false;
        }

        private static bool AllTrained(Pawn p)
        {
            var tracker = p.training;
            if (tracker == null) return false;
            var trainability = p.RaceProps?.trainability;
            if (trainability == null || trainability == TrainabilityDefOf.None) return false;
            bool any = false;
            foreach (var td in TrainableUtility.TrainableDefsInListOrder)
            {
                if (td.requiredTrainability == null) continue;
                if (td.requiredTrainability.intelligenceOrder > trainability.intelligenceOrder) continue;
                any = true;
                if (!tracker.HasLearned(td)) return false;
            }
            return any;
        }

        // isBad=true → negative traits; isBad=false → positive traits.
        private static bool HasGoodBadTrait(Pawn p, bool isBad)
        {
            foreach (var def in AnimalTraitsAccess.KnownTraitDefs())
            {
                if (AnimalTraitsAccess.HasTrait(p, def) && def.isBad == isBad)
                    return true;
            }
            return false;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref has, "has", true);
            Scribe_Defs.Look(ref trait, "trait");
            Scribe_Defs.Look(ref disease, "disease");
            Scribe_Defs.Look(ref trainable, "trainable");
            Scribe_Values.Look(ref inheritMode, "inheritMode", InheritableFilter.Both);
        }
    }
}
