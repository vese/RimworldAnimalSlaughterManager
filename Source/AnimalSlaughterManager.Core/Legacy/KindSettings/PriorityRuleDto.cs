#pragma warning disable CS0612, CS0618
using System;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// The pre-discriminator XML form of one priority rule (a single attribute-driven DTO for
/// every rule type, read back through reflection). Delete together with the rest of the
/// Legacy folder when the save-compatibility horizon allows.
/// </summary>
[Obsolete]
public class PriorityRuleDto
{
    [XmlAttribute]
    public string rule;
    [XmlAttribute]
    public bool has = true;
    [XmlAttribute]
    public string trait;
    [XmlAttribute]
    public string disease;
    [XmlAttribute]
    public string trainable;
    [XmlAttribute]
    public string generalType;
    [XmlAttribute]
    public string inheritability;

    public BasePriorityRule? ToRule()
    {
        var type = Type.GetType("ASM." + rule, false);

        if (type == null)
        {
            return null;
        }

        var instance = (BasePriorityRule?)Activator.CreateInstance(type);

        if (instance == null)
        {
            return null;
        }

        foreach (var field in typeof(PriorityRuleDto).GetFields())
        {
            if (field.Name == nameof(rule))
            {
                continue;
            }

            var target = type.GetField(field.Name, BindingFlags.Public | BindingFlags.Instance);

            if (target == null)
            {
                continue;
            }

            var value = field.GetValue(this);

            if (target.FieldType == typeof(bool) || value == null)
            {
                continue; // bool keeps its default; nulls keep theirs
            }

            if (target.FieldType.IsSubclassOf(typeof(Def)))
            {
                var def = ResolveDef(target.FieldType, (string)value);
                target.SetValue(instance, def);
            }
            else if (target.FieldType.IsEnum)
            {
                if (Enum.TryParse(target.FieldType, (string)value, out var parsed))
                {
                    target.SetValue(instance, parsed);
                }
            }
        }

        return instance;
    }

    private static Def? ResolveDef(Type defType, string defName) =>
        GenDefDatabase.GetAllDefsInDatabaseForDef(defType).FirstOrDefault(d => d.defName == defName);
}
