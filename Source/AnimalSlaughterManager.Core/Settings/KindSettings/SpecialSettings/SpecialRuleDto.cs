using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ASM;

/// <summary>XML-serializable form of a special rule for preset files (stores defNames).</summary>
public class SpecialRuleDto
{
    [XmlAttribute]
    public string target;
    [XmlAttribute]
    public int keepCount = 1;
    [XmlAttribute]
    public string ageScope = nameof(ASM.AgeScope.Both);
    [XmlAttribute]
    public string genderScope = "Any";
    [XmlAttribute]
    public string inheritMode = "";

    public static SpecialRuleDto From(SpecialRule r) => new()
    {
        target = r.target?.defName,
        ageScope = r.ageScope.ToString(),
        genderScope = r.genderScope.ToString(),
        inheritMode = r.inheritMode
    };

    public static SpecialRuleDto From(KeepRule r) => new()
    {
        target = r.target?.defName,
        keepCount = r.keepCount,
        ageScope = r.ageScope.ToString(),
        genderScope = r.genderScope.ToString(),
        inheritMode = r.inheritMode
    };

    public KeepRule ToKeep() => new()
    {
        target = RuleDtoDefs.ByName(target),
        keepCount = keepCount,
        ageScope = Parse(ageScope, ASM.AgeScope.Both),
        genderScope = Parse(genderScope, GenderScope.Any),
        inheritMode = inheritMode
    };

    public ForceCullRule ToCull() => new()
    {
        target = RuleDtoDefs.ByName(target),
        ageScope = Parse(ageScope, ASM.AgeScope.Both),
        genderScope = Parse(genderScope, GenderScope.Any),
        inheritMode = inheritMode
    };

    private static T Parse<T>(string value, T fallback) where T : struct =>
        System.Enum.TryParse(value, out T parsed) ? parsed : fallback;
}

/// <summary>Shared lookup: the target's def type is owned by the integration.</summary>
public static class RuleDtoDefs
{
    public static global::Verse.Def? ByName(string? defName) =>
        defName == null ? null :
        global::Verse.GenDefDatabase.GetAllDefsInDatabaseForDef(typeof(global::Verse.Def))
            .FirstOrDefault(d => d.defName == defName);
}

/// <summary>The special-rules section of a kind preset.</summary>
[XmlRoot("SpecialRules")]
public class SpecialRulesSectionDto
{
    [XmlElement("Rule")]
    public List<SpecialRuleDto> KeepRules = [];

    [XmlElement("Rule")]
    public List<SpecialRuleDto> ForceCullRules = [];

    public static SpecialRulesSectionDto From(KindSpecialRules rules) => new()
    {
        KeepRules = rules.Keep.rules.Select(SpecialRuleDto.From).ToList(),
        ForceCullRules = rules.ForceCull.rules.Select(SpecialRuleDto.From).ToList()
    };
}

/// <summary>A single keep-list element of a List-scope preset.</summary>
[XmlRoot("KeepRules")]
public class KeepRulesListDto
{
    [XmlElement("Rule")]
    public List<SpecialRuleDto> Rule = [];
}

/// <summary>A single force-cull-list element of a List-scope preset.</summary>
[XmlRoot("ForceCullRules")]
public class ForceCullRulesListDto
{
    [XmlElement("Rule")]
    public List<SpecialRuleDto> Rule = [];
}
