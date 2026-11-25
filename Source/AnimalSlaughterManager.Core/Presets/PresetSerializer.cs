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
/// The preset format layer: (de)serialization of the preset DTOs, root-element dispatch
/// between the current and historical formats, and the versioning — the preset is stamped
/// with PresetFormat.CurrentVersion on save, versions above the current are refused, older
/// ones migrate in ReadPreset. RimWorld is touched only by the refusal warning.
/// </summary>
public static class PresetSerializer
{
    // XmlSerializer generates a serialization assembly on first use — cache the instances.
    /// <summary>Root element names pairing the [XmlRoot] values of the preset DTOs; the
    /// dispatcher matches them against a file root.</summary>
    public const string PresetRoot = "SlaughterPreset";
    public const string RulePresetRoot = "RulePresetDto";
    public const string AllPresetRoot = "AllPreset";
    public const string KindPresetRoot = "KindPreset";
    public const string ListPresetRoot = "ListPreset";

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

    public static PresetDto? ReadPreset(string? text)
    {
        var dto = Deserialize<PresetDto>(text);

        if (dto != null && dto.Version > PresetFormat.CurrentVersion)
        {
            Log.Warning("ASM: skipped a preset saved by a newer mod version.");
            return null;
        }

        if (dto?.Kinds.Any(k => k.PrioRulesAdultMale?.Length > 0 || k.PrioRulesYoungMale?.Length > 0
            || k.PrioRulesAdultFemale?.Length > 0 || k.PrioRulesYoungFemale?.Length > 0) == true)
        {
            return Migrate(dto);
        }

#pragma warning disable CS0618
        // An old format has no rule lists — convert it; a current prefs-only preset stays
        // as parsed.
        return LegacyPresets.TryReadPreset(text) ?? (dto == null ? null : Migrate(dto));
#pragma warning restore CS0618
    }

    /// <summary>Brings a preset of an older version up to the current model. Version 0 is
    /// the shape at the introduction of versioning — nothing to migrate yet; add a step per
    /// bump of PresetFormat.CurrentVersion.</summary>
    private static PresetDto Migrate(PresetDto dto)
    {
        switch (dto.Version)
        {
            case 0:
                break;
        }

        dto.Version = PresetFormat.CurrentVersion;
        return dto;
    }

#pragma warning disable CS0618
    public static List<PriorityRuleDto>? ReadRulePreset(string? text)
    {
        var dto = Deserialize<RulePresetDto>(text);
        return dto?.rules?.ToList();
#pragma warning restore CS0618
    }

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

    /// <summary>Root element name of the file — identifies which of the historical formats it is.</summary>
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

    /// <summary>Writes a preset DTO of the current version (the caller picks the folder; the
    /// root element is the DTO's own).</summary>
    public static string SerializePreset<T>(T dto) where T : class, IVersionedPresetDto
    {
        dto.Version = PresetFormat.CurrentVersion;

        var sb = new StringBuilder();
        using (var w = new StringWriter(sb))
        {
            Serializer<T>().Serialize(w, dto);
        }

        return sb.ToString();
    }

    /// <summary>Reads a preset DTO of the current format; null when the text is not one.</summary>
    public static T? DeserializePreset<T>(string? text) where T : class, IVersionedPresetDto => Deserialize<T>(text);
}
