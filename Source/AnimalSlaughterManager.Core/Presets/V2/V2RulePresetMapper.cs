using System;
using System.Linq;
using System.Reflection;

namespace ASM;

/// <summary>Maps a rule's Settings DTO to and from its V2 file model (found by the "V2" name
/// convention; the fields copy by name and type — the copies are transitional until the
/// shapes diverge).</summary>
public static class V2RulePresetMapper
{
    public static object? ToV2(BasePriorityRulePreset dto)
    {
        var type = FindType("ASM.V2" + dto.GetType().Name);

        if (type == null)
        {
            return null;
        }

        var v2 = Activator.CreateInstance(type)!;
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

    private static Type? FindType(string fullName) =>
        AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName, false)).FirstOrDefault(t => t != null);

    private static void Copy(object from, object to)
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

            var value = source is PropertyInfo p2 ? p2.GetValue(from) : ((FieldInfo)source).GetValue(from);

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
