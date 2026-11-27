#pragma warning disable CS0612, CS0618

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>
/// The version-1 rule-payload machinery: the RulePresetFile wrappers (the rule name plus the
/// serialized DTO) read from a bucket element's Rule children, and the DTOs from their
/// payloads. Delete together with the rest of the Legacy folder.
/// </summary>
[Obsolete]
public static class LegacyRulePresets
{
    private static readonly Dictionary<Type, XmlSerializer> serializers = [];

    private static XmlSerializer GetSerializer(Type type)
    {
        if (!serializers.TryGetValue(type, out var ser))
        {
            ser = new XmlSerializer(type);
            serializers[type] = ser;
        }

        return ser;
    }

    /// <summary>Rule-preset wrappers from a bucket element's Rule children.</summary>
    public static List<RulePresetFile> ReadBucket(XmlElement? bucket)
    {
        var presets = new List<RulePresetFile>();

        if (bucket == null)
        {
            return presets;
        }

        foreach (XmlElement ruleElement in bucket.ChildNodes)
        {
            if (ruleElement.LocalName != "Rule")
            {
                continue;
            }

            presets.Add(new RulePresetFile
            {
                Type = ruleElement.GetAttribute(nameof(RulePresetFile.Type)),
                Payload = [.. ruleElement.ChildNodes.Cast<XmlElement>()]
            });
        }

        return presets;
    }

    /// <summary>DTOs from the payloads: each payload deserializes into its rule's DTO.</summary>
    public static List<BasePriorityRulePreset> ToDtos(IEnumerable<RulePresetFile> presets)
    {
        var result = new List<BasePriorityRulePreset>();

        foreach (var preset in presets)
        {
            var rule = Type.GetType("ASM." + preset.Type, false);
            var dtoType = rule == null ? null : rule.GetMethod("ToPreset")?.ReturnType;

            if (dtoType == null)
            {
                continue;
            }

            var dto = DeserializePayload(preset.Payload, dtoType);

            if (dto is BasePriorityRulePreset typed)
            {
                result.Add(typed);
            }
        }

        return result;
    }

    private static object? DeserializePayload(XmlElement[]? payload, Type dtoType)
    {
        if (payload == null || payload.Length == 0)
        {
            return null;
        }

        try
        {
            using var reader = new StringReader(payload[0].OuterXml);
            return GetSerializer(dtoType).Deserialize(reader);
        }
        catch
        {
            return null;
        }
    }
}
