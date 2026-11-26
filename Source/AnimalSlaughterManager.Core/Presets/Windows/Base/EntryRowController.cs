using System;

using Verse;

namespace ASM;

/// <summary>The row's policy from the browser: the marks and gating, the apply and overwrite
/// actions, and the label the no-slice message quotes.</summary>
internal sealed class EntryRowController(
    Func<PresetEntry, string?> getMark,
    Func<PresetEntry, string?> getTip,
    Func<PresetEntry, bool> canOverwrite,
    Func<PresetEntry, bool> canDelete,
    Func<PresetEntry, string> getConfirmDeleteText,
    Func<PresetEntry, bool> apply,
    Action<PresetEntry> overwrite,
    TaggedString targetLabel)
{
    public Func<PresetEntry, string?> GetMark => getMark;
    public Func<PresetEntry, string?> GetTip => getTip;
    public Func<PresetEntry, bool> CanOverwrite => canOverwrite;
    public Func<PresetEntry, bool> CanDelete => canDelete;
    public Func<PresetEntry, string> GetConfirmDeleteText => getConfirmDeleteText;
    public Func<PresetEntry, bool> Apply => apply;
    public Action<PresetEntry> Overwrite => overwrite;
    public TaggedString TargetLabel => targetLabel;
}
