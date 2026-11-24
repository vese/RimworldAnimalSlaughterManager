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
/// The preset format layer: (de)serialization of the DTO carriers, root-element dispatch
/// between the current and historical formats, and the versioning — the carrier is stamped
/// with PresetFormat.CurrentVersion on save, versions above the current are refused, older
/// ones migrate in ReadCarrier. RimWorld is touched only by the refusal warning.
/// </summary>
public static class PresetSerializer
{
    // XmlSerializer generates a serialization assembly on first use — cache the instances.
    /// <summary>Root element names pairing the [XmlRoot] values of the DTO carriers; the
    /// dispatcher matches them against a file root.</summary>
    public const string CarrierRoot = "SlaughterPreset";
    public const string RulePresetRoot = "RulePresetDto";

    private static readonly Dictionary<Type, XmlSerializer> serializers = new();

    public static XmlSerializer Serializer<T>() where T : class
    {
        if (!serializers.TryGetValue(typeof(T), out var ser))
        {
            ser = new XmlSerializer(typeof(T));
            serializers[typeof(T)] = ser;
        }

        return ser;
    }

    public static string SerializeToText(SlaughterPresetDto dto)
    {
        dto.version = PresetFormat.CurrentVersion;

        var sb = new StringBuilder();
        using (var w = new StringWriter(sb))
        {
            Serializer<SlaughterPresetDto>().Serialize(w, dto);
        }

        return sb.ToString();
    }

    public static SlaughterPresetDto? ReadCarrier(string? text)
    {
        var dto = Deserialize<SlaughterPresetDto>(text);

        if (dto != null && dto.version > PresetFormat.CurrentVersion)
        {
            Log.Warning("ASM: skipped a preset saved by a newer mod version.");
            return null;
        }

        if (dto?.Kinds.Any(k => k.PrioRulesAdultMale?.Count > 0 || k.PrioRulesYoungMale?.Count > 0
            || k.PrioRulesAdultFemale?.Count > 0 || k.PrioRulesYoungFemale?.Count > 0) == true)
        {
            return Migrate(dto);
        }

#pragma warning disable CS0618
        // An old carrier has no rule lists — convert it; a current prefs-only preset stays
        // as parsed.
        return LegacyPresets.TryReadCarrier(text) ?? (dto == null ? null : Migrate(dto));
#pragma warning restore CS0618
    }

    /// <summary>Brings a carrier of an older version up to the current model. Version 0 is
    /// the shape at the introduction of versioning — nothing to migrate yet; add a step per
    /// bump of PresetFormat.CurrentVersion.</summary>
    private static SlaughterPresetDto Migrate(SlaughterPresetDto dto)
    {
        switch (dto.version)
        {
            case 0:
                break;
        }

        dto.version = PresetFormat.CurrentVersion;
        return dto;
    }

    public static List<PriorityRuleDto>? ReadRulePreset(string? text)
    {
        var dto = Deserialize<RulePresetDto>(text);
        return dto?.rules?.ToList();
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
}
