namespace ASM;

/// <summary>The version-1 conversions: the old root-element formats, read through the Legacy
/// machinery; writing is not supported (v1 is never written).</summary>
public interface IV1PresetConverter<TModel> : IPresetConverter<TModel> where TModel : BasePreset
{
}
