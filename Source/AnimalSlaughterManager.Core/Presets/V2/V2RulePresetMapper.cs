using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;

namespace ASM;

/// <summary>Maps a rule's Settings DTO to and from its V2 file model (found by the "V2" name
/// convention; the fields copy by name and type — the copies are transitional until the
/// shapes diverge).</summary>
public static class V2RulePresetMapper
{
    public static V2RulePreset? ToV2(BasePriorityRulePreset dto)
    {
        var type = FindType("ASM.V2" + dto.GetType().Name);

        if (type == null)
        {
            return null;
        }

        var v2 = (V2RulePreset)Activator.CreateInstance(type)!;
        Copy(dto, v2);

        return v2;
    }

    public static BasePriorityRulePreset? ToDto(object v2)
    {
        var type = FindType("ASM." + v2.GetType().Name["V2".Length..]);

        if (type == null || Activator.CreateInstance(type) is not BasePriorityRulePreset dto)
        {
            return null;
        }

        Copy(v2, dto);

        return dto;
    }

    public static Type? V2TypeOf(Type dtoType) => FindType("ASM.V2" + dtoType.Name);

    internal static Type? FindSettingsType(Type v2Type) => FindType("ASM." + v2Type.Name["V2".Length..]);

    internal static Type? FindType(string fullName) =>
        AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName, false)).FirstOrDefault(t => t != null);

    /// <summary>A list member whose element type has a "V2" counterpart maps element-wise.</summary>
    private static object? MemberValue(MemberInfo source, object from, Type targetType)
    {
        var sourceType = source is PropertyInfo p ? p.PropertyType : ((FieldInfo)source).FieldType;

        if (sourceType.IsGenericType && targetType.IsGenericType
            && sourceType.GetGenericTypeDefinition() == typeof(List<>)
            && targetType.GetGenericTypeDefinition() == typeof(List<>)
            && (targetType.GetGenericArguments()[0].Name == "V2" + sourceType.GetGenericArguments()[0].Name
                || sourceType.GetGenericArguments()[0].Name == "V2" + targetType.GetGenericArguments()[0].Name))
        {
            var sourceList = (System.Collections.IEnumerable)(source is PropertyInfo pp ? pp.GetValue(from) : ((FieldInfo)source).GetValue(from))!;
            var list = (System.Collections.IList)Activator.CreateInstance(targetType)!;
            var elemV2 = targetType.GetGenericArguments()[0];

            foreach (var item in sourceList)
            {
                var v2 = Activator.CreateInstance(elemV2)!;
                Copy(item, v2);
                list.Add(v2);
            }

            return list;
        }

        return null;
    }

    internal static void Copy(object from, object to)
    {
        foreach (var member in to.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance))
        {
            if (member is not (FieldInfo or PropertyInfo) || (member as PropertyInfo)?.CanWrite == false)
            {
                continue;
            }

            var targetType = (member as FieldInfo)?.FieldType ?? ((PropertyInfo)member).PropertyType;
            var source = from.GetType().GetProperty(member.Name, BindingFlags.Public | BindingFlags.Instance)
                ?? (MemberInfo?)from.GetType().GetField(member.Name, BindingFlags.Public | BindingFlags.Instance);

            var sourceType = source switch
            {
                PropertyInfo p => p.CanRead ? p.PropertyType : null,
                FieldInfo f => f.FieldType,
                _ => null
            };

            if (sourceType != targetType)
            {
                continue;
            }

            var value = MemberValue(source, from, targetType) ?? (source is PropertyInfo p2 ? p2.GetValue(from) : ((FieldInfo)source).GetValue(from));

            if (member is FieldInfo field)
            {
                field.SetValue(to, value);
            }
            else
            {
                ((PropertyInfo)member).SetValue(to, value);
            }
        }
    }
}
