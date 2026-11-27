using Verse;

namespace ASM;

/// <summary>Contributes a section to the shared "Special rules" tab of the kind settings
/// window (the tab itself lives in Core). Registered unconditionally, so a disabled mod
/// still explains itself through the notice.</summary>
public interface ISpecialSettingsSectionSource
{
    /// <summary>False when there is nothing to edit (the mod is missing or has no content):
    /// the tab shows <see cref="MissingModNotice"/> instead of the section and sinks this
    /// source below the available ones.</summary>
    bool Available { get; }

    /// <summary>The notice replacing the section UI when <see cref="Available"/> is false.</summary>
    TaggedString MissingModNotice { get; }

    /// <summary>Creates the section UI for the given kind; null when there is nothing to edit.</summary>
    ISpecialSettingsSection? CreateSection(KindSettings settings);
}
