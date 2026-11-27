using System.Linq;

namespace ASM;

/// <summary>The converter of a Settings preset model for the file's format version; the
/// implementations register themselves in ServiceRegistry under their version
/// interface.</summary>
internal static class PresetConverterFactory
{
    public static IPresetConverter<TModel>? GetConverter<TModel>(int version)
        where TModel : BasePreset =>
        version == 1
            ? ServiceRegistry.GetServices<IV1PresetConverter<TModel>>().FirstOrDefault()
            : ServiceRegistry.GetServices<IV2PresetConverter<TModel>>().FirstOrDefault();
}
