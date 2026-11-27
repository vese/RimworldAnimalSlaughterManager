using System.Linq;

namespace ASM;

/// <summary>The converter of a Settings preset model for the file's format version; the
/// implementations register themselves in IntegrationServices under their version
/// interface.</summary>
internal static class PresetConverters
{
    public static IPresetConverter<TModel>? For<TModel>(PresetFile file)
        where TModel : class =>
        file.Version == 1
            ? IntegrationServices.GetServices<IV1PresetConverter<TModel>>().FirstOrDefault()
            : IntegrationServices.GetServices<IV2PresetConverter<TModel>>().FirstOrDefault();
}
