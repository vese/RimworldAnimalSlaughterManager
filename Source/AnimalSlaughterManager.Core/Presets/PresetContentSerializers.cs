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
    private static readonly Dictionary<Type, XmlSerializer> serializers = [];

    private static readonly Type[] Subtypes =
    [
        ..AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeTypes)
            .Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(V2RulePreset)) || t.IsSubclassOf(typeof(V2SpecialSettingsPreset))))
    ];

    public static XmlSerializer GetSerializer(Type type)
    {
        if (!serializers.TryGetValue(type, out var ser))
        {
            ser = new XmlSerializer(type, Subtypes);
            serializers[type] = ser;
        }

        return ser;
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

}
