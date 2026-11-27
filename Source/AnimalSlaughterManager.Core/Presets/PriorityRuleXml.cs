using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The rule-list (de)serialization of preset files: the Settings models carry the
/// concrete rule DTOs (plain objects); this layer converts them into the file form — the
/// rule class name plus the DTO serialized as the payload (XmlSerializer of the concrete
/// type). Reading routes the payload by the name back into the DTO and fills the rule.
/// The pre-discriminator attribute-form Rule elements are read as legacy.</summary>
public static class PriorityRuleXml
{
    // DTO type -> (rule name, factory), discovered once by probing the rule types.
    private static readonly Dictionary<Type, (string RuleName, Func<BasePriorityRule> Factory)> dtoMap = [];

    /// <summary>The file form of the DTOs: the rule class name plus each serialized DTO.</summary>
    public static List<RulePresetFile> ToRulePresets(IEnumerable<BasePriorityRulePreset> presets)
    {
        var result = new List<RulePresetFile>();

        foreach (var preset in presets)
        {
            var doc = new XmlDocument();
            using (var writer = doc.CreateNavigator()!.AppendChild())
            {
                PresetSerializer.GetSerializer(preset.GetType()).Serialize(writer, preset);
            }

            result.Add(new RulePresetFile { Type = GetRuleName(preset.GetType()), Payload = [.. doc.ChildNodes.Cast<XmlElement>()] });
        }

        return result;
    }

    /// <summary>Rules from the DTOs: each DTO fills its rule (found by the DTO's rule).
    /// Unknown DTOs are dropped.</summary>
    public static List<BasePriorityRule> ToRules(IEnumerable<BasePriorityRulePreset> presets)
    {
        var result = new List<BasePriorityRule>();

        foreach (var preset in presets)
        {
            var rule = GetRule(preset.GetType());

            if (rule != null)
            {
                rule.FromPreset(preset);
                result.Add(rule);
            }
        }

        return result;
    }

    /// <summary>Rules from a bucket element's children (the RulePresetFile elements of the current
    /// format, or the attribute-form Rule elements of the older one).</summary>
    public static List<BasePriorityRule> ReadRules(XmlElement? bucket)
    {
        var presets = new List<RulePresetFile>();

        if (bucket != null)
        {
            foreach (XmlElement child in bucket.ChildNodes)
            {
                var preset = ReadRulePreset(child);

                if (preset != null)
                {
                    presets.Add(preset);
                }
            }
        }

        return ToRules(ToDtos(presets));
    }

    /// <summary>DTOs from the file form: each payload deserializes into its rule's DTO.</summary>
    public static List<BasePriorityRulePreset> ToDtos(IEnumerable<RulePresetFile> presets)
    {
        var result = new List<BasePriorityRulePreset>();

        foreach (var preset in presets)
        {
            var entry = RuleByName(preset.Type);

            if (entry == null)
            {
                continue;
            }

            var dto = (BasePriorityRulePreset?)DeserializePayload(preset.Payload, entry.Value.Factory().ToPreset().GetType());

            if (dto != null)
            {
                result.Add(dto);
            }
        }

        return result;
    }

    private static RulePresetFile? ReadRulePreset(XmlElement child)
    {
        if (child.LocalName == "Rule")
        {
            try
            {
                using var reader = new StringReader(child.OuterXml);
                return (RulePresetFile?)PresetSerializer.GetSerializer(typeof(RulePresetFile)).Deserialize(reader);
            }
            catch
            {
                return null;
            }
        }

#pragma warning disable CS0618
        // The pre-discriminator format: the attribute-driven DTO of all rules.
        if (!string.IsNullOrEmpty(child.GetAttribute("rule")))
        {
            try
            {
                using var reader = new StringReader(child.OuterXml);
                var dto = (PriorityRuleDto)new XmlSerializer(typeof(PriorityRuleDto), new XmlRootAttribute(child.LocalName)).Deserialize(reader);

                var rule = dto.ToRule();

                return rule == null ? null : ToRulePresets([rule.ToPreset()])[0];
            }
            catch
            {
                return null;
            }
        }
#pragma warning restore CS0618

        return null;
    }

    private static object? DeserializePayload(XmlElement[]? payload, Type? presetType)
    {
        if (payload == null || payload.Length == 0 || presetType == null)
        {
            return null;
        }

        try
        {
            using var reader = new StringReader(payload[0].OuterXml);
            return PresetSerializer.GetSerializer(presetType).Deserialize(reader);
        }
        catch
        {
            return null;
        }
    }

    private static string GetRuleName(Type dtoType)
    {
        EnsureMap();

        return dtoMap.TryGetValue(dtoType, out var entry) ? entry.RuleName : "";
    }

    private static BasePriorityRule? GetRule(Type dtoType)
    {
        EnsureMap();

        if (!dtoMap.TryGetValue(dtoType, out var entry))
        {
            return null;
        }

        var rule = entry.Factory();
        return rule;
    }

    private static (string RuleName, Func<BasePriorityRule> Factory)? RuleByName(string ruleName)
    {
        var type = Type.GetType("ASM." + ruleName, false);

        if (type == null || !typeof(BasePriorityRule).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
        {
            return null;
        }

        return (ruleName, () => (BasePriorityRule)Activator.CreateInstance(type)!);
    }

    // Probes every rule type once: its ToPreset() reveals the DTO type.
    private static void EnsureMap()
    {
        if (dtoMap.Count > 0)
        {
            return;
        }

        foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
        {
            if (!typeof(BasePriorityRule).IsAssignableFrom(type) || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
            {
                continue;
            }

            var rule = (BasePriorityRule)Activator.CreateInstance(type)!;
            var dto = rule.ToPreset();
            dtoMap[dto.GetType()] = (type.Name, () => (BasePriorityRule)Activator.CreateInstance(type)!);
        }
    }
}

/// <summary>Rule presets read from a bucket element's Rule children (the inverse of the
/// file writers' bucket serialization).</summary>
public static class RulePresetXml
{
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

    public static XmlElement Bucket(string name, List<RulePresetFile> presets)
    {
        var doc = new XmlDocument();
        var bucket = doc.CreateElement(name);
        doc.AppendChild(bucket);

        foreach (var preset in presets)
        {
            var rule = doc.CreateElement("Rule");
            rule.SetAttribute(nameof(RulePresetFile.Type), preset.Type);
            bucket.AppendChild(rule);

            foreach (var payload in preset.Payload ?? [])
            {
                rule.AppendChild(doc.ImportNode(payload, true));
            }
        }

        return bucket;
    }
}
