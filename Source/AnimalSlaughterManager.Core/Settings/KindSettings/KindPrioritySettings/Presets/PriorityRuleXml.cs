using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>Reading and writing the rule lists of a preset bucket: every rule appends its own
/// DTO element (the element name is the type discriminator), the reader maps the name to a
/// factory — the core's own rules and the integrations' (IPriorityRuleTypes). Children of the
/// pre-discriminator format (the attribute-driven Rule element) are read as legacy.</summary>
public static class PriorityRuleXml
{
    /// <summary>A bucket element with the rules' DTO children.</summary>
    public static XmlElement Bucket(string name, IEnumerable<BasePriorityRule> rules)
    {
        var doc = new XmlDocument();
        var bucket = doc.CreateElement(name);
        doc.AppendChild(bucket);

        WriteRules(bucket, rules);

        return bucket;
    }

    public static void WriteRules(XmlElement parent, IEnumerable<BasePriorityRule> rules)
    {
        foreach (var rule in rules)
        {
            rule.WriteXml(parent);
        }
    }

    /// <summary>Rules from a bucket element's children; unknown rules and rules whose defs did
    /// not resolve are dropped.</summary>
    public static List<BasePriorityRule> ReadRules(XmlElement? bucket)
    {
        var rules = new List<BasePriorityRule>();

        if (bucket == null)
        {
            return rules;
        }

        foreach (XmlElement child in bucket.ChildNodes)
        {
            var rule = ReadRule(child);

            if (rule != null && !rule.HasNullDef)
            {
                rules.Add(rule);
            }
        }

        return rules;
    }

    private static BasePriorityRule? ReadRule(XmlElement child)
    {
        var factory = FactoryOf(child.LocalName);

        if (factory != null)
        {
            var rule = factory();
            rule.ReadXml(child);
            return rule;
        }

#pragma warning disable CS0618
        // The pre-discriminator format: the attribute-driven DTO of all rules.
        if (!string.IsNullOrEmpty(child.GetAttribute("rule")))
        {
            try
            {
                using var reader = new StringReader(child.OuterXml);
                var dto = (PriorityRuleDto)new XmlSerializer(typeof(PriorityRuleDto), new XmlRootAttribute(child.LocalName)).Deserialize(reader);

                return dto.ToRule();
            }
            catch
            {
                return null;
            }
        }
#pragma warning restore CS0618

        return null;
    }

    private static Func<BasePriorityRule>? FactoryOf(string dtoName)
    {
        foreach (var source in IntegrationServices.GetServices<IPriorityRuleTypes>())
        {
            foreach (var (name, factory) in source.Types)
            {
                if (name == dtoName)
                {
                    return factory;
                }
            }
        }

        return null;
    }

    /// <summary>Fills a list preset with the rules' DTO children.</summary>
    public static void WriteRulesInto(ListPresetDto dto, IEnumerable<BasePriorityRule> rules)
{
    var doc = new XmlDocument();
    var root = doc.CreateElement("Rules");
    doc.AppendChild(root);
    WriteRules(root, rules);
    dto.Content = [root];
}
}
