using System.Collections.Generic;

namespace ASM;

/// <summary>The version-2 file model of the ATS special settings, mirroring the Settings
/// TraitsPreset (mapped by name).</summary>
public class V2TraitsPreset : V2SpecialSettingsPreset
{
    public List<TraitDto> KeepTraits = [];

    public List<TraitDto> ForceCullTraits = [];
}
