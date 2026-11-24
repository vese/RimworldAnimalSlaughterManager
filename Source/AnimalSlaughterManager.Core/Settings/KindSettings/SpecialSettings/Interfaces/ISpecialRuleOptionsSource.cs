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

    /// <summary>Serialized names of the integration's inheritability values; empty when the
    /// integration has no such option (the rules' dropdown is hidden... column kept).</summary>
    IReadOnlyList<string> InheritModes { get; }

    TaggedString LabelOfInheritMode(string mode);

    Color ColorOf(Def target);

    string TipOf(Def target);
}
