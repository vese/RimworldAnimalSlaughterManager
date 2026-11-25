using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The rule-list (de)serialization of preset files: every rule preset writes its own
/// element (the element name is the type discriminator), the reader maps the name back to the
/// preset type through IPriorityRuleTypes. The pre-discriminator attribute-form Rule elements
/// are read as legacy.</summary>
public static class PriorityRuleXml
{
    // name -> preset type, probed once per discriminator through the rule factories.
    private static readonly Dictionary<string, Type> presetTypes = [];

    /// <summary>A bucket element with the rule presets' elements inside.</summary>
    public static XmlElement Bucket(string name, IEnumerable<PriorityRulePreset> presets)
    {
        var doc = new XmlDocument();
        var bucket = doc.CreateElement(name);
        doc.AppendChild(bucket);

        WritePresets(bucket, presets);

        return bucket;
    }

    public static void WritePresets(XmlElement parent, IEnumerable<PriorityRulePreset> presets)
    {
        foreach (var preset in presets)
        {
            using var writer = parent.OwnerDocument!.CreateNavigator()!.AppendChild();
            SerializerOf(preset.GetType()).Serialize(writer, preset);
        }
    }

    /// <summary>Rule presets from a bucket element's children; unknown children are dropped.</summary>
    public static List<PriorityRulePreset> ReadPresets(XmlElement? bucket)
    {
        var presets = new List<PriorityRulePreset>();

        if (bucket == null)
        {
            return presets;
        }

        foreach (XmlElement child in bucket.ChildNodes)
        {
            var preset = ReadPreset(child);

            if (preset != null)
            {
                presets.Add(preset);
            }
        }

        return presets;
    }

    /// <summary>Rules from rule presets (each preset fills its rule through the registry).</summary>
    public static List<BasePriorityRule> ToRules(IEnumerable<PriorityRulePreset> presets)
    {
        var rules = new List<BasePriorityRule>();

        foreach (var preset in presets)
        {
            var rule = RuleOf(preset.DtoName);

            if (rule != null)
            {
                rule.FromPreset(preset);
                rules.Add(rule);
            }
        }

        return rules;
    }

    /// <summary>Rule presets of rules.</summary>
    public static List<PriorityRulePreset> ToPresets(IEnumerable<BasePriorityRule> rules) =>
        [.. rules.Select(r => r.ToPreset())];

    /// <summary>Rules from a bucket element's children; unknown rules and rules whose defs did
    /// not resolve are dropped.</summary>
    public static List<BasePriorityRule> ReadRules(XmlElement? bucket) => ToRules(ReadPresets(bucket));

    private static PriorityRulePreset? ReadPreset(XmlElement child)
    {
        var type = PresetTypeOf(child.LocalName);

        if (type != null)
        {
            try
            {
                using var reader = new StringReader(child.OuterXml);
                return (PriorityRulePreset?)SerializerOf(type).Deserialize(reader);
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

                return dto.ToRule()?.ToPreset();
            }
            catch
            {
                return null;
            }
        }
#pragma warning restore CS0618

        // An integration section or an unknown element — not an error here.
        return null;
    }

    private static BasePriorityRule? RuleOf(string dtoName)
    {
        foreach (var source in IntegrationServices.GetServices<IPriorityRuleTypes>())
        {
            foreach (var (name, factory) in source.Types)
            {
                if (name == dtoName)
                {
                    return factory();
                }
            }
        }

        return null;
    }

    private static Type? PresetTypeOf(string dtoName)
    {
        if (presetTypes.TryGetValue(dtoName, out var type))
        {
            return type;
        }

        var rule = RuleOf(dtoName);
        type = rule?.ToPreset().GetType();

        if (type != null)
        {
            presetTypes[dtoName] = type;
        }

        return type;
    }


    private static XmlSerializer SerializerOf(Type presetType) => PresetSerializer.SerializerOf(presetType);
}
