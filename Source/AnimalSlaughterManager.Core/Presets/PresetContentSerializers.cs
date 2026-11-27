using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The content serializers of the V2 file models: their polymorphic members (the rule
/// and special-settings DTOs) need every registered subtype passed to the XmlSerializer
/// constructor — the subtypes are discovered over the loaded assemblies once and the
/// serializers are cached per root type. Also bridges the special-list sections between
/// elements and their typed models (a section subclass's [XmlRoot] is its element name).</summary>
public static class PresetContentSerializers
{
    private static Type[]? subtypes;

    private static readonly Dictionary<Type, XmlSerializer> serializers = [];

    private static readonly Dictionary<string, Type> sectionTypes = [];

    private static Type[] Subtypes => subtypes ??= AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(SafeTypes)
        .Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(V2RulePreset)) || t.IsSubclassOf(typeof(V2SpecialSettingsPreset))))
        .ToArray();

    public static XmlSerializer SerializerOf(Type type)
    {
        if (!serializers.TryGetValue(type, out var ser))
        {
            ser = new XmlSerializer(type, Subtypes);
            serializers[type] = ser;
        }

        return ser;
    }

    /// <summary>The model serialized as its root element (for the section-based legacy
    /// consumers).</summary>
    public static List<XmlElement> ToElements(object content)
    {
        var doc = new XmlDocument();

        using (var writer = doc.CreateNavigator()!.AppendChild())
        {
            SerializerOf(content.GetType()).Serialize(writer, content);
        }

        return [.. doc.ChildNodes.Cast<XmlElement>()];
    }

    /// <summary>The model's serialized children — the per-list section elements (the save
    /// format of a special list).</summary>
    public static List<XmlElement> ToChildElements(object content) =>
        [.. ToElements(content)[0].ChildNodes.Cast<XmlElement>()];

    /// <summary>A section element as its typed model (matched by [XmlRoot] over the registered
    /// V2SpecialListSection subclasses); null for a foreign section.</summary>
    public static V2SpecialListSection? ToSection(XmlElement element)
    {
        if (!sectionTypes.TryGetValue(element.LocalName, out var type))
        {
            sectionTypes[element.LocalName] = type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeTypes)
                .FirstOrDefault(t => !t.IsAbstract && t.IsSubclassOf(typeof(V2SpecialListSection))
                    && t.GetCustomAttribute<XmlRootAttribute>()?.ElementName == element.LocalName) ?? typeof(None);
        }

        if (type == typeof(None))
        {
            return null;
        }

        try
        {
            using var reader = new StringReader(element.OuterXml);
            return (V2SpecialListSection?)SerializerOf(type).Deserialize(reader);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The typed section model as its element.</summary>
    public static XmlElement ToElement(V2SpecialListSection section)
    {
        var doc = new XmlDocument();

        using (var writer = doc.CreateNavigator()!.AppendChild())
        {
            SerializerOf(section.GetType()).Serialize(writer, section);
        }

        return doc.DocumentElement!;
    }

    /// <summary>The Settings DTO's V2 file model (the "V2" name convention), converted through
    /// their shared serialized shape — no field copying.</summary>
    public static object? ToV2(object dto)
    {
        var target = TypeByName("V2" + dto.GetType().Name);

        if (target == null)
        {
            return null;
        }

        var sb = new System.Text.StringBuilder();
        using (var w = new StringWriter(sb))
        {
            SerializerOf(dto.GetType()).Serialize(w, dto);
        }

        try
        {
            using var r = new StringReader(sb.ToString());
            return SerializerOf(target).Deserialize(r);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The V2 file model's Settings DTO, converted the same way.</summary>
    public static object? ToSettings(object v2)
    {
        var target = TypeByName(v2.GetType().Name["V2".Length..]);

        if (target == null)
        {
            return null;
        }

        var sb = new System.Text.StringBuilder();
        using (var w = new StringWriter(sb))
        {
            SerializerOf(v2.GetType()).Serialize(w, v2);
        }

        try
        {
            using var r = new StringReader(sb.ToString());
            return SerializerOf(target).Deserialize(r);
        }
        catch
        {
            return null;
        }
    }

    private static readonly Dictionary<string, Type?> typesByName = [];

    private static Type? TypeByName(string name)
    {
        if (!typesByName.TryGetValue(name, out var type))
        {
            typesByName[name] = type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeTypes)
                .FirstOrDefault(t => t.Name == name);
        }

        return type;
    }

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null)!;
        }
    }

    private sealed class None : V2SpecialListSection;
}
