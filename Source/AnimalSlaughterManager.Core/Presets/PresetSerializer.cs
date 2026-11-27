using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Verse;

namespace ASM;

/// <summary>
/// The preset format layer: every current-format preset is one file form (PresetFile — the
/// version, the metadata and the serialized content model), so reading is two plain steps
/// with no root-element guessing. Historical formats are recognized by their root elements
/// and read by the v1 machinery. The version is stamped on save, versions above the current
/// are refused. RimWorld is touched only by the refusal warning.
/// </summary>
public static class PresetSerializer
{
    /// <summary>The current format's root element; everything else is a historical format.</summary>
    public const string CurrentRoot = "Preset";

    // The historical root elements the v1 dispatch still matches.
    public const string V1PresetRoot = "SlaughterPreset";
    public const string V1RuleRoot = "RulePresetDto";

    // XmlSerializer generates a serialization assembly on first use — cache the instances.
    private static readonly Dictionary<Type, XmlSerializer> serializers = new();

    /// <summary>Cached serializer of a preset model type (the DTO element name is its root).</summary>
    public static XmlSerializer SerializerOf(Type type)
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


#pragma warning disable CS0618
    public static List<PriorityRuleDto>? ReadRulePreset(string? text)
    {
        var dto = Deserialize<RulePresetDto>(text);
        return dto?.rules?.ToList();
    }

    private static KindPreset? V1ToCurrent(KindPresetDto v1) => KindPresetFile.FromV1(v1)?.Kind;
#pragma warning restore CS0618

    private static T? Deserialize<T>(string? text) where T : class
    {
        if (text == null)
        {
            return null;
        }

        try
        {
            using (var r = new StringReader(text))
            {
                return (T)Serializer<T>().Deserialize(r);
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Root element name of the file — identifies a historical format.</summary>
    public static string? RootElementOf(string? text)
    {
        if (text == null)
        {
            return null;
        }

        try
        {
            using (var reader = XmlReader.Create(new StringReader(text)))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        return reader.LocalName;
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }

    /// <summary>Writes a preset of the current format: the content model wrapped in the file
    /// form carrying the version and the metadata.</summary>
    public static string SerializePreset(object content, PresetScope scope)
    {
        var file = new PresetFile
        {
            Version = PresetFormat.CurrentVersion,
            Scope = scope,
            Type = content.GetType().Name,
            Content = SerializeContent(content)
        };

        var sb = new StringBuilder();
        using (var w = new StringWriter(sb))
        {
            SerializerOf(typeof(PresetFile)).Serialize(w, file);
        }

        return sb.ToString();
    }

    /// <summary>The file form of a current-format preset; refuses versions above the current one
    /// (a preset saved by a newer mod), null when the text is not one.</summary>
    public static PresetFile? ReadPreset(string? text)
    {
        if (RootElementOf(text) != CurrentRoot)
        {
            return null;
        }

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

    /// <summary>The kind slices of a version-1 or older preset: the v1 carrier's own, or the
    /// old format's converted; null when the text is not a preset.</summary>
#pragma warning disable CS0618
    public static List<KindPreset>? ReadV1Kinds(string? text)
    {
        var dto = Deserialize<PresetDto>(text);

        if (dto?.Kinds.Any(k => k.PrioRulesAdultMale?.Length > 0 || k.PrioRulesYoungMale?.Length > 0
            || k.PrioRulesAdultFemale?.Length > 0 || k.PrioRulesYoungFemale?.Length > 0) == true)
        {
            return [.. dto.Kinds.Select(V1ToCurrent).Where(k => k != null)];
        }

        // An old format has no rule lists — its kinds come converted; a current prefs-only
        // v1 preset stays as parsed.
        return LegacyPresets.TryReadPreset(text) ?? (dto == null ? null : [.. dto.Kinds.Select(V1ToCurrent).Where(k => k != null)]);
    }
#pragma warning restore CS0618

    private static string SerializeContent(object content)
    {
        var sb = new StringBuilder();
        using (var w = new StringWriter(sb))
        {
            SerializerOf(content.GetType()).Serialize(w, content);
        }

        return sb.ToString();
    }
}
