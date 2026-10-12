using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The ATS section of a kind preset: the keep and force-cull trait lists.</summary>
[XmlRoot("atsTraits")]
public class TraitsSectionDto
{
    [XmlArray("KeepTraits")]
    [XmlArrayItem("Trait")]
    public List<TraitDto> KeepTraits = new();

    [XmlArray("ForceCullTraits")]
    [XmlArrayItem("Trait")]
    public List<TraitDto> ForceCullTraits = new();
}

/// <summary>A single keep list — the List-scope preset form (also the legacy flat child).</summary>
[XmlRoot("KeepTraits")]
public class KeepTraitsListDto
{
    [XmlElement("Trait")]
    public List<TraitDto> Trait = new();
}

/// <summary>A single force-cull list — the List-scope preset form (also the legacy flat child).</summary>
[XmlRoot("ForceCullTraits")]
public class ForceCullTraitsListDto
{
    [XmlElement("Trait")]
    public List<TraitDto> Trait = new();
}
