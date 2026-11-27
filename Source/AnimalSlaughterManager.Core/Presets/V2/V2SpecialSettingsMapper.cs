using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

namespace ASM;

/// <summary>Maps an integration's special-settings Settings DTO to and from its V2 file model
/// (found by the "V2" name convention; the members copy by name — see V2RulePresetMapper for
/// the rules).</summary>
public static class V2SpecialSettingsMapper
{
    public static V2SpecialSettingsPreset? ToV2(string key, BaseSpecialSettingsPreset preset)
    {
        var type = V2RulePresetMapper.V2TypeOf(preset.GetType());

        if (type == null || Activator.CreateInstance(type) is not V2SpecialSettingsPreset v2)
        {
            return null;
        }

        v2.Key = key;
        V2RulePresetMapper.Copy(preset, v2);

        return v2;
    }

    public static BaseSpecialSettingsPreset? ToSettings(V2SpecialSettingsPreset v2)
    {
        var type = V2RulePresetMapper.FindSettingsType(v2.GetType());

        if (type == null || Activator.CreateInstance(type) is not BaseSpecialSettingsPreset preset)
        {
            return null;
        }

        V2RulePresetMapper.Copy(v2, preset);

        return preset;
    }
    private static readonly Dictionary<string, Type> sectionTypes = [];

    public static V2SpecialListSection? ToSection(System.Xml.XmlElement element)
    {
        if (!sectionTypes.TryGetValue(element.LocalName, out var type))
        {
            sectionTypes[element.LocalName] = type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; } })
                .FirstOrDefault(t => !t.IsAbstract && t.IsSubclassOf(typeof(V2SpecialListSection))
                    && t.GetCustomAttribute<System.Xml.Serialization.XmlRootAttribute>()?.ElementName == element.LocalName) ?? typeof(None);
        }

        if (type == typeof(None))
        {
            return null;
        }

        try
        {
            using var reader = new System.IO.StringReader(element.OuterXml);
            return (V2SpecialListSection?)PresetSerializer.GetSerializer(type).Deserialize(reader);
        }
        catch
        {
            return null;
        }
    }

    public static System.Xml.XmlElement ToElement(V2SpecialListSection section)
    {
        var doc = new System.Xml.XmlDocument();

        using (var writer = doc.CreateNavigator()!.AppendChild())
        {
            PresetSerializer.GetSerializer(section.GetType()).Serialize(writer, section);
        }

        return doc.DocumentElement!;
    }

    private sealed class None : V2SpecialListSection;
}
