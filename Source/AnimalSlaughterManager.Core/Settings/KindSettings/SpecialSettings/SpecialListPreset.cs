using System.Collections.Generic;

namespace ASM;

/// <summary>The special-settings list as the Settings-side model: the integration settings by
/// their provider keys — a single-list preset carries its provider's settings with only that
/// list populated.</summary>
public class SpecialListPreset : BasePreset
{
    public Dictionary<string, BaseSpecialSettingsPreset> Settings = [];
}
