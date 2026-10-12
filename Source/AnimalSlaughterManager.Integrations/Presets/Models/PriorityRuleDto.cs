using System.Linq;
using System;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

/// <summary>
/// XML form of one priority rule: the rule type name plus its parameters. Deliberately
/// type-agnostic — the parameters copy by field name through reflection, so rules from any
/// assembly (integration rules included) round-trip without this DTO referencing their types.
/// Field conventions: defs as defName strings (trait → HediffDef, trainable → TrainableDef,
/// disease → HediffDef), enums as names.
/// </summary>
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

    public static PriorityRuleDto From(BasePriorityRule r)
    {
        var dto = new PriorityRuleDto { rule = r.GetType().Name };

        foreach (var field in typeof(PriorityRuleDto).GetFields())
        {
            if (field.Name == nameof(rule))
            {
                continue;
            }

            var source = r.GetType().GetField(field.Name, BindingFlags.Public | BindingFlags.Instance);

            if (source == null)
            {
                continue;
            }

            var value = source.GetValue(r);
            field.SetValue(dto, value switch
            {
                null => null,
                Def def => def.defName,
                Enum e => e.ToString(),
                bool b => b,
                _ => null,
            });
        }

        return dto;
    }

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
