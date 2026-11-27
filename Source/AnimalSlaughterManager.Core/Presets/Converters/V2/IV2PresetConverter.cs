namespace ASM;

/// <summary>The current (version-2) conversions: the V2 file models of Presets/V2.</summary>
public interface IV2PresetConverter<TModel> : IPresetConverter<TModel> where TModel : BasePreset
{
}
