using System.Xml;
using Verse;

namespace ASM;

/// <summary>
/// The base of the per-kind special settings models (e.g. the ATS keep/force-cull trait
/// lists). The core stores them opaquely under the owning integration's key — in the save
/// (inside the KindSettings node) and in kind presets (as the integration's own XML section)
/// — and never names their concrete type.
/// </summary>
public abstract class SpecialSettingsPreset : IExposable
{
    /// <summary>True when the settings deviate from the default and the kind must be treated
    /// as customized.</summary>
    public abstract bool Customized { get; }

    public abstract void Reset();

    public abstract void ExposeData();

    /// <summary>Writes the preset slice: appends the integration's section element (named by
    /// the provider's key) with its contents under <paramref name="parent"/>.</summary>
    public abstract void WriteXml(XmlElement parent);

    /// <summary>Reads the settings from their own section (or its legacy flat children) among
    /// the kind element's children; true when a section of this integration was there.</summary>
    public abstract bool ReadXml(XmlElement kind);
}
