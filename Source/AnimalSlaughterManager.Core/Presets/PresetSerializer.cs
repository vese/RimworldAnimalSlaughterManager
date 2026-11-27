using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Verse;

namespace ASM;

/// <summary>
/// The preset format layer: every current-format preset is one file form (PresetFile — the
/// version, the metadata and the serialized content model), so reading is two plain steps
/// with no root-element guessing; versions above the current are refused. The historical
/// formats live in Legacy. RimWorld is touched only by the refusal warning.
/// </summary>
public static class PresetSerializer
{
    // XmlSerializer generates a serialization assembly on first use — cache the instances.
    private static readonly Dictionary<Type, XmlSerializer> serializers = new();

    /// <summary>Cached serializer of a preset model type (the DTO element name is its root).</summary>
    public static XmlSerializer GetSerializer(Type type)
    {
        if (!serializers.TryGetValue(type, out var ser))
        {
            ser = new XmlSerializer(type);
            serializers[type] = ser;
        }

        return ser;
    }

    public static XmlSerializer Serializer<T>() where T : class
    {
        if (!serializers.TryGetValue(typeof(T), out var ser))
        {
            ser = new XmlSerializer(typeof(T));
            serializers[typeof(T)] = ser;
        }

        return ser;
    }

    /// <summary>Deserialized text as T; null when the text is null or not parsable.</summary>
    public static T? Deserialize<T>(string? text) where T : class
    {
        if (text == null)
        {
            return null;
        }

        try
        {
            using var r = new StringReader(text);
            return (T)Serializer<T>().Deserialize(r);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Writes a preset of the current format: the content model wrapped in the file
    /// form carrying the version and the metadata (the scope, the list bucket).</summary>
    public static string SerializePreset(object content, PresetScope scope, string? bucket = null) =>
        Serialize(new PresetFile
        {
            Version = PresetFormat.CurrentVersion,
            Scope = scope,
            Bucket = bucket,
            Type = content.GetType().Name,
            Content = SerializeContent(content)
        });

    /// <summary>The file form's text.</summary>
    public static string Serialize(PresetFile file)
    {
        var sb = new StringBuilder();
        using (var w = new StringWriter(sb))
        {
            GetSerializer(typeof(PresetFile)).Serialize(w, file);
        }

        return sb.ToString();
    }

    /// <summary>The content model serialized to XML text.</summary>
    public static string SerializeContent(object content)
    {
        var sb = new StringBuilder();
        using (var w = new StringWriter(sb))
        {
            GetSerializer(content.GetType()).Serialize(w, content);
        }

        return sb.ToString();
    }

    /// <summary>The file form of a current-format preset. A foreign root simply fails to
    /// deserialize into it (the root lives in [XmlRoot] alone); versions above the current one
    /// (a preset saved by a newer mod) are refused. Null when the text is not one.</summary>
    public static PresetFile? ReadPreset(string? text)
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
    public static T? ReadContent<T>(PresetFile file) where T : class =>
        file.Type == typeof(T).Name ? Deserialize<T>(file.Content) : null;

}
