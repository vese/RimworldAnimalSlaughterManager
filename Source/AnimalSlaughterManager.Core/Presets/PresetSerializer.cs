using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Verse;

namespace ASM;

/// <summary>
/// The preset format layer: every current-format preset is one file form (PresetFile — the
/// version, the metadata and the serialized content model), so reading is two plain steps
/// with no root-element guessing; versions above the current are refused. The content models'
/// polymorphic members (the rule and special-settings models) need every registered subtype
/// passed to the XmlSerializer constructor — the subtypes are discovered over the loaded
/// assemblies once and the serializers are cached per root type. The historical formats live
/// in Legacy. RimWorld is touched only by the refusal warning.
/// </summary>
public static class PresetSerializer
{
    // XmlSerializer generates a serialization assembly on first use — cache the instances.
    private static readonly Dictionary<Type, XmlSerializer> serializers = [];

    private static readonly Type[] Subtypes =
    [
        ..AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeTypes)
            .Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(V2RulePreset)) || t.IsSubclassOf(typeof(V2SpecialSettingsPreset))))
    ];

    /// <summary>The file form's text.</summary>
    public static string Serialize(PresetFile file)
    {
        var sb = new StringBuilder();

        using (var w = new StringWriter(sb))
        {
            SerializerOf(typeof(PresetFile)).Serialize(w, file);
        }

        return sb.ToString();
    }

    /// <summary>The content model serialized to XML text.</summary>
    public static string SerializeContent(object content)
    {
        var sb = new StringBuilder();

        using (var w = new StringWriter(sb))
        {
            SerializerOf(content.GetType()).Serialize(w, content);
        }

        return sb.ToString();
    }

    /// <summary>The file form of a current-format preset. A foreign root simply fails to
    /// deserialize into it (the root lives in [XmlRoot] alone); versions above the current one
    /// (a preset saved by a newer mod) are refused. Null when the text is not one.</summary>
    public static PresetFile? Deserialize(string? text)
    {
        var file = Deserialize<PresetFile>(text);

        if (file != null && file.Version > PresetFormat.CurrentVersion)
        {
            Log.Warning("ASM: skipped a preset saved by a newer mod version.");
            return null;
        }

        return file;
    }

    /// <summary>The file's content as the model; null when the type name does not match (the
    /// preset is not of the expected shape).</summary>
    public static T? DeserializeContent<T>(PresetFile file) where T : class =>
        file.Type == typeof(T).Name ? Deserialize<T>(file.Content) : null;

    /// <summary>The cached serializer of a model type, aware of every registered subtype.</summary>
    internal static XmlSerializer SerializerOf(Type type)
    {
        if (!serializers.TryGetValue(type, out var ser))
        {
            ser = new XmlSerializer(type, Subtypes);
            serializers[type] = ser;
        }

        return ser;
    }

    /// <summary>Deserialized text as T; null when the text is null or not parsable.</summary>
    private static T? Deserialize<T>(string? text) where T : class
    {
        if (text == null)
        {
            return null;
        }

        try
        {
            using var r = new StringReader(text);
            return (T)SerializerOf(typeof(T)).Deserialize(r);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null)!;
        }
    }
}
