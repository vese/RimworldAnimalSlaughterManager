using System.Collections.Generic;

namespace ASM;

/// <summary>The version-2 file model of the ATS special settings, mirroring the Settings
/// TraitsPreset (mapped by name, the trait lists element-wise).</summary>
public class V2TraitsPreset : V2SpecialSettingsPreset
{
    public List<V2TraitDto> KeepTraits = [];

    public List<V2TraitDto> ForceCullTraits = [];
}
