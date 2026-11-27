using System.Collections.Generic;

using Verse;

namespace ASM;

/// <summary>
/// Self-registration of the preset converters: RimWorld runs every [StaticConstructorOnStartup]
/// type, and each Settings model's version conversions join the registry under their version
/// interface (resolved by PresetConverterFactory through ServiceRegistry).
/// </summary>
[StaticConstructorOnStartup]
public static class PresetsRegistration
{
    static PresetsRegistration()
    {
        ServiceRegistry.Add<IV2PresetConverter<AllKindsPreset>>(new V2AllKindsPresetConverter());
        ServiceRegistry.Add<IV2PresetConverter<KindPreset>>(new V2KindPresetConverter());
        ServiceRegistry.Add<IV2PresetConverter<List<BasePriorityRulePreset>>>(new V2PriorityListPresetConverter());
        ServiceRegistry.Add<IV2PresetConverter<SpecialListPreset>>(new V2SpecialListPresetConverter());

        ServiceRegistry.Add<IV1PresetConverter<AllKindsPreset>>(new V1AllKindsPresetConverter());
        ServiceRegistry.Add<IV1PresetConverter<KindPreset>>(new V1KindPresetConverter());
    }
}
