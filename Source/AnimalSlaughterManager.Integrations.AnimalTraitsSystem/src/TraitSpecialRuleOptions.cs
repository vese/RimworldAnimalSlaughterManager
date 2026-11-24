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

    // The rules' extra option column is the ATS inheritability filter.
    public bool HasExtraOption => true;

    public TaggedString ExtraOptionLabel => ASMKeys.InheritMode.Translate();

    public IDropdownController GetExtraDropdown(SpecialRule rule) => new DropdownController<string>
    {
        Current = ParseInherit(rule.inheritMode).ToString(),
        Values = Enum.GetNames(typeof(TraitInheritability)),
        LabelOf = mode => mode.Translate(),
        SetValue = mode => rule.SetInheritMode(mode)
    };

    private static TraitInheritability ParseInherit(string mode) =>
        Enum.TryParse(mode, out TraitInheritability parsed) ? parsed : TraitInheritability.Both;

    public Color ColorOf(Def target) => target is HediffDef def ? AnimalTraitsAccess.TraitColor(def) : Color.white;

    public string TipOf(Def target) => target is HediffDef def ? AnimalTraitsAccess.TraitTip(def) : "";
}
