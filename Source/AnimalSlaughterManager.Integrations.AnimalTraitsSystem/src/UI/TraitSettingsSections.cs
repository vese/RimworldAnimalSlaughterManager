using Verse;

namespace ASM;

/// <summary>The ATS source of the shared "Special rules" tab: keep and force-cull trait lists
/// of the kind, or a notice when there are no traits to work with.</summary>
public sealed class TraitSettingsSections : IKindSettingsSectionSource
{
    public bool Available => AnimalTraitsAccess.HasAvailableTraits;

    public TaggedString MissingModNotice => ASMKeys.ATSNotDetected.Translate();

    public IKindSettingsSection? CreateSection(KindSettings settings) =>
        new TraitSettingsSection(KindTraitsSettings.Of(settings));
}
