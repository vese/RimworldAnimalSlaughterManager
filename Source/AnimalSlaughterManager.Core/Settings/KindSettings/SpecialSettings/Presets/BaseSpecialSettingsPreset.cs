namespace ASM;

/// <summary>
/// The base of the per-kind special settings models (e.g. the ATS keep/force-cull trait
/// lists): a plain data container. Everything else — queries, reset, the save scribing, the
/// preset XML section — is the owning provider's business (the concrete type may implement
/// IExposable for the save's LookMode.Deep, but the model itself carries no behavior).
/// </summary>
public abstract class BaseSpecialSettingsPreset
{
}
