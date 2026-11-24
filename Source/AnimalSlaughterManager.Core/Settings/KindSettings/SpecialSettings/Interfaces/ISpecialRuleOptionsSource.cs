using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>Supplies the targets the special-rules lists are built from (an integration's
/// content): the picker, the row presentation, and the integration's own inheritability
/// values. The lists themselves live in the core and are always shown.</summary>
public interface ISpecialRuleOptionsSource
{
    /// <summary>False when the integration's mod is missing: the lists stay, the add buttons
    /// carry the notice instead.</summary>
    bool Available { get; }

    TaggedString MissingModNotice { get; }

    /// <summary>Opens the integration's target picker; every picked target becomes a rule of
    /// the list the button belongs to.</summary>
    void OpenPicker(Action<List<Def>> picked);

    /// <summary>True when the rules carry the integration's own extra option, rendered as one
    /// more dropdown column after age/gender (e.g. an inheritability filter).</summary>
    bool HasExtraOption { get; }

    /// <summary>Column caption of the extra option for the header row.</summary>
    TaggedString ExtraOptionLabel { get; }

    /// <summary>The extra-option dropdown of a rule (reads/writes the rule's serialized
    /// value); called only when HasExtraOption.</summary>
    IDropdownController GetExtraDropdown(SpecialRule rule);

    Color ColorOf(Def target);

    string TipOf(Def target);
}
