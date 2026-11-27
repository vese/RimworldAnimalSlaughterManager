using System;

namespace ASM;

/// <summary>Maps an integration's special-settings Settings DTO to and from its V2 file model
/// (found by the "V2" name convention; the members copy by name — see V2RulePresetMapper for
/// the rules).</summary>
public static class V2SpecialSettingsMapper
{
    public static V2SpecialSettingsPreset? ToV2(string key, BaseSpecialSettingsPreset preset)
    {
        var type = V2RulePresetMapper.V2TypeOf(preset.GetType());

        if (type == null || Activator.CreateInstance(type) is not V2SpecialSettingsPreset v2)
        {
            return null;
        }

        v2.Key = key;
        V2RulePresetMapper.Copy(preset, v2);

        return v2;
    }

    public static BaseSpecialSettingsPreset? ToSettings(V2SpecialSettingsPreset v2)
    {
        var type = V2RulePresetMapper.FindSettingsType(v2.GetType());

        if (type == null || Activator.CreateInstance(type) is not BaseSpecialSettingsPreset preset)
        {
            return null;
        }

        V2RulePresetMapper.Copy(v2, preset);

        return preset;
    }
}
