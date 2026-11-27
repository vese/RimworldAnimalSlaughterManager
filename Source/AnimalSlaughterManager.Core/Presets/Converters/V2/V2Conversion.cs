using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ASM;

/// <summary>The V2 converters' shared round trip: a Settings DTO and its V2 file model (the
/// "V2" name convention) share the serialized shape — serialize one, deserialize as the
/// other; no field copying.</summary>
internal static class V2Conversion
{
    private static readonly Dictionary<string, Type?> typesByName = [];

    public static object? ToV2(object dto) => Convert(dto, "V2" + dto.GetType().Name);

    public static object? ToSettings(object v2) => Convert(v2, v2.GetType().Name["V2".Length..]);

    private static object? Convert(object value, string targetName)
    {
        var target = TypeByName(targetName);

        if (target == null)
        {
            return null;
        }

        var sb = new System.Text.StringBuilder();
        using (var w = new StringWriter(sb))
        {
            PresetContentSerializers.SerializerOf(value.GetType()).Serialize(w, value);
        }

        try
        {
            using var r = new StringReader(sb.ToString());
            return PresetContentSerializers.SerializerOf(target).Deserialize(r);
        }
        catch
        {
            return null;
        }
    }

    private static Type? TypeByName(string name)
    {
        if (!typesByName.TryGetValue(name, out var type))
        {
            typesByName[name] = type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; } })
                .FirstOrDefault(t => t.Name == name);
        }

        return type;
    }
}
