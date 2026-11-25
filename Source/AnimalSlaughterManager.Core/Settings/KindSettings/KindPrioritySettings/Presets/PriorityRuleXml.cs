using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The rule-list (de)serialization of preset files: a rule becomes a RulePreset —
/// the rule class name plus its own DTO serialized as the payload (XmlSerializer of the
/// concrete type; no shared DTO base). Reading routes the payload by the name through
/// IPriorityRuleTypes. The pre-discriminator attribute-form Rule elements are read as
/// legacy.</summary>
public static class PriorityRuleXml
{
    // rule class name -> (factory, preset type), probed once through the rule factories.
    private static readonly Dictionary<string, (Func<BasePriorityRule> Factory, Type PresetType)> rules = [];

    /// <summary>Rule presets of rules: the name plus the serialized DTO.</summary>
    public static List<RulePreset> ToRulePresets(IEnumerable<BasePriorityRule> list)
    {
        var result = new List<RulePreset>();

        foreach (var rule in list)
        {
            var preset = rule.ToPreset();
            var doc = new XmlDocument();
            using (var writer = doc.CreateNavigator()!.AppendChild())
            {
                PresetSerializer.SerializerOf(preset.GetType()).Serialize(writer, preset);
            }

            result.Add(new RulePreset { Type = rule.GetType().Name, Payload = [.. doc.ChildNodes.Cast<XmlElement>()] });
        }

        return result;
    }

    /// <summary>Rules from rule presets; each payload deserializes into its rule's DTO and
    /// fills the rule. Unknown names and unreadable payloads are dropped.</summary>
    public static List<BasePriorityRule> ToRules(IEnumerable<RulePreset> presets)
    {
        var result = new List<BasePriorityRule>();

        foreach (var preset in presets)
        {
            var entry = RuleOf(preset.Type);

            if (entry == null)
            {
                continue;
            }

            var dto = DeserializePayload(preset.Payload, entry.Value.PresetType);

            if (dto != null)
            {
                var rule = entry.Value.Factory();
                rule.FromPreset(dto);
                result.Add(rule);
            }
        }

        return result;
    }

    /// <summary>Rules from a bucket element's children (the RulePreset elements of the current
    /// format, or the attribute-form Rule elements of the older one).</summary>
    public static List<BasePriorityRule> ReadRules(XmlElement? bucket)
    {
        var presets = new List<RulePreset>();

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

        return ToRules(presets);
    }

    private static RulePreset? ReadRulePreset(XmlElement child)
    {
        if (child.LocalName == "Rule")
        {
            try
            {
                using var reader = new StringReader(child.OuterXml);
                return (RulePreset?)PresetSerializer.SerializerOf(typeof(RulePreset)).Deserialize(reader);
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

                return rule == null ? null : ToRulePresets([rule])[0];
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
            return PresetSerializer.SerializerOf(presetType).Deserialize(reader);
        }
        catch
        {
            return null;
        }
    }

    private static (Func<BasePriorityRule> Factory, Type PresetType)? RuleOf(string ruleName)
    {
        if (rules.TryGetValue(ruleName, out var entry))
        {
            return entry;
        }

        var factory = FactoryOf(ruleName);

        if (factory == null)
        {
            return null;
        }

        entry = (factory, factory().ToPreset().GetType());
        rules[ruleName] = entry;

        return entry;
    }

    private static Type? PresetTypeOf(string ruleName) => RuleOf(ruleName)?.PresetType;

    private static Func<BasePriorityRule>? FactoryOf(string ruleName)
    {
        foreach (var source in IntegrationServices.GetServices<IPriorityRuleTypes>())
        {
            foreach (var (name, factory) in source.Types)
            {
                if (name == ruleName)
                {
                    return factory;
                }
            }
        }

        return null;
    }
}

/// <summary>Rule presets read from a bucket element's Rule children (the inverse of the
/// file writers' bucket serialization).</summary>
public static class RulePresetXml
{
    public static List<RulePreset> ReadBucket(System.Xml.XmlElement? bucket)
    {
        var presets = new List<RulePreset>();

        if (bucket == null)
        {
            return presets;
        }

        foreach (System.Xml.XmlElement ruleElement in bucket.ChildNodes)
        {
            if (ruleElement.LocalName != "Rule")
            {
                continue;
            }

            presets.Add(new RulePreset
            {
                Type = ruleElement.GetAttribute(nameof(RulePreset.Type)),
                Payload = [.. ruleElement.ChildNodes.Cast<System.Xml.XmlElement>()]
            });
        }

        return presets;
    }

    public static System.Xml.XmlElement Bucket(string name, System.Collections.Generic.List<RulePreset> presets)
    {
        var doc = new System.Xml.XmlDocument();
        var bucket = doc.CreateElement(name);
        doc.AppendChild(bucket);

        foreach (var preset in presets)
        {
            var rule = doc.CreateElement("Rule");
            rule.SetAttribute(nameof(RulePreset.Type), preset.Type);
            bucket.AppendChild(rule);

            foreach (var payload in preset.Payload ?? [])
            {
                rule.AppendChild(doc.ImportNode(payload, true));
            }
        }

        return bucket;
    }
}
