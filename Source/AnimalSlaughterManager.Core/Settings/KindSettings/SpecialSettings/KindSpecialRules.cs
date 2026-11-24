using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace ASM;

/// <summary>
/// The kind's special rules: the keep (breeding protection) and force-cull lists. Owned by the
/// core and always shown on the special settings tab; integrations supply the targets to pick
/// from through ISpecialRuleOptionsSource. In kind presets they ride as the "specialRules"
/// XML section.
/// </summary>
public class KindSpecialRules : IExposable
{
    public const string SectionName = "specialRules";

    public KeepRuleSet Keep = new();
    public ForceCullRuleSet ForceCull = new();

    public bool Customized => Keep.HasRules || ForceCull.HasRules;

    public void Reset()
    {
        Keep.Clear();
        ForceCull.Clear();
    }

    public void ExposeData()
    {
        Keep.ExposeData("keepRules");
        ForceCull.ExposeData("forceCullRules");
    }

    public void WriteXml(XmlElement parent)
    {
        PresetSectionSerialization.Serialize(parent, SpecialRulesSectionDto.From(this));
    }

    public void ReadXml(XmlElement kind)
    {
        var section = kind[SectionName];

        if (section == null)
        {
            return;
        }

        var dto = PresetSectionSerialization.Deserialize<SpecialRulesSectionDto>(section);

        if (dto == null)
        {
            return;
        }

        Fill(dto.KeepRules, dto.ForceCullRules);
    }

    public void FillKeep(List<SpecialRuleDto> rules)
    {
        foreach (var r in rules.Select(d => d.ToKeep()))
        {
            if (r.target != null)
            {
                Keep.rules.Add(r);
            }
        }
    }

    public void FillCull(List<SpecialRuleDto> rules)
    {
        foreach (var r in rules.Select(d => d.ToCull()))
        {
            if (r.target != null)
            {
                ForceCull.rules.Add(r);
            }
        }
    }

    private void Fill(System.Collections.Generic.List<SpecialRuleDto> keep, System.Collections.Generic.List<SpecialRuleDto> cull)
    {
        Keep.rules.Clear();
        ForceCull.rules.Clear();
        FillKeep(keep);
        FillCull(cull);
    }
}

public class KeepRuleSet : SpecialRuleSet<KeepRule>
{
    public override string PresetsFolder => "Keep";

    public override string ListElementName => "KeepRules";

    public override void WriteXml(XmlElement parent)
    {
        PresetSectionSerialization.Serialize(parent, new KeepRulesListDto { Rule = rules.Select(SpecialRuleDto.From).ToList() });
    }

    public override bool ReadXml(XmlElement kind)
    {
        var list = kind[ListElementName];

        if (list == null)
        {
            return false;
        }

        rules.Clear();

        foreach (var r in (PresetSectionSerialization.Deserialize<KeepRulesListDto>(list)?.Rule ?? []).Select(d => d.ToKeep()))
        {
            if (r.target != null)
            {
                rules.Add(r);
            }
        }

        return true;
    }
}

public class ForceCullRuleSet : SpecialRuleSet<ForceCullRule>
{
    public override string PresetsFolder => "ForceCull";

    public override string ListElementName => "ForceCullRules";

    public override void WriteXml(XmlElement parent)
    {
        PresetSectionSerialization.Serialize(parent, new ForceCullRulesListDto { Rule = rules.Select(SpecialRuleDto.From).ToList() });
    }

    public override bool ReadXml(XmlElement kind)
    {
        var list = kind[ListElementName];

        if (list == null)
        {
            return false;
        }

        rules.Clear();

        foreach (var r in (PresetSectionSerialization.Deserialize<ForceCullRulesListDto>(list)?.Rule ?? []).Select(d => d.ToCull()))
        {
            if (r.target != null)
            {
                rules.Add(r);
            }
        }

        return true;
    }
}
