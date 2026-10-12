using System.Collections.Generic;
using Verse;

namespace ASM;

/// <summary>
/// What the tabs need from their host window's map component: per-kind settings access, the
/// map-wide settings, and the recompute request. Implemented by the main assembly's map
/// component — the shared tab code stays free of main-assembly types.
/// </summary>
public interface IKindSettingsHost
{
    KindSettings GetSettings(ThingDef def);

    GlobalSettings GlobalSettings { get; }

    void MarkDirty();

    /// <summary>Every configured kind with its settings.</summary>
    IEnumerable<KeyValuePair<ThingDef, KindSettings>> AllKindSettings { get; }
}
