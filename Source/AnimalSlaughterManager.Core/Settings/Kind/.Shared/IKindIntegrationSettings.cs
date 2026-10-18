using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using Verse;

namespace ASM;

/// <summary>
/// Per-kind settings owned by an integration (e.g. the ATS keep/force-cull trait lists). The
/// core stores them opaquely under the integration's key — in the save (inside the KindSettings
/// node) and in kind presets (as the integration's own XML section) — and never names their type.
/// </summary>
public interface IKindIntegrationSettings : IExposable
{
    /// <summary>True when the integration's settings deviate from the default and the kind
    /// must be treated as customized.</summary>
    bool Customized { get; }

    void Reset();

    /// <summary>Writes the preset slice: appends the integration's section element (named by
    /// the provider's key) with its contents under <paramref name="parent"/>.</summary>
    void WriteXml(XmlElement parent);

    /// <summary>Reads the preset slice from a Kind element: the integration's own section, or
    /// the legacy flat children saved by earlier versions.</summary>
    void ReadXml(XmlElement kind);
}

