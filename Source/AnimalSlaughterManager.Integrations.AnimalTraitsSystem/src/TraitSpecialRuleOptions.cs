using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>The ATS targets of the core's special-rules lists: the traits to keep or cull,
/// their colors and tooltips, the inheritability values, and the trait picker.</summary>
public sealed class TraitSpecialRuleOptions : ISpecialRuleOptionsSource
{
    public bool Available => AnimalTraitsAccess.HasAvailableTraits;

    public TaggedString MissingModNotice => ASMKeys.ATSNotDetected.Translate();

    public void OpenPicker(Action<List<Def>> picked) =>
        Find.WindowStack.Add(new Dialog_TraitPicker(traits => picked([.. traits])));

    public IReadOnlyList<string> InheritModes =>
        [.. Enum.GetNames(typeof(TraitInheritability))];

    public TaggedString LabelOfInheritMode(string mode) => mode.Translate();

    public Color ColorOf(Def target) => target is HediffDef def ? AnimalTraitsAccess.TraitColor(def) : Color.white;

    public string TipOf(Def target) => target is HediffDef def ? AnimalTraitsAccess.TraitTip(def) : "";
}
