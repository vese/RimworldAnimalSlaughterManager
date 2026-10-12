using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>
/// The preset format layer, pure BCL: (de)serialization of the DTO carriers, root-element
/// dispatch between the current and historical formats, and the condition-bucket mapping.
/// No RimWorld types here — this is the unit-testable core under <see cref="PresetRepository"/>.
/// </summary>
public static class PresetSerializer
{
    // XmlSerializer generates a serialization assembly on first use — cache the instances.
    /// <summary>Root element names pairing the [XmlRoot] values of the DTO carriers; the
    /// dispatcher matches them against a file root.</summary>
    public const string CarrierRoot = "SlaughterPreset";
    public const string RulePresetRoot = "RulePresetDto";
    public const string ConditionPresetRoot = "ConditionPreset";

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
        var sb = new StringBuilder();
        using (var w = new StringWriter(sb))
        {
            Serializer<SlaughterPresetDto>().Serialize(w, dto);
        }

        return sb.ToString();
    }

    public static SlaughterPresetDto? ReadCarrier(string? text)
    {
        return Deserialize<SlaughterPresetDto>(text);
    }

    public static List<PriorityRuleDto>? ReadRulePreset(string? text)
    {
        var dto = Deserialize<RulePresetDto>(text);
        return dto?.rules?.ToList();
    }

    public static List<ConditionDto>? ReadConditionPreset(string? text)
    {
        var dto = Deserialize<ConditionPresetDto>(text);
        return dto?.conditions?.ToList();
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
