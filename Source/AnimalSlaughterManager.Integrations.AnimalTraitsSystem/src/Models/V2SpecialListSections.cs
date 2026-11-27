using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-2 file model of the keep-list section of a special-list preset.</summary>
[XmlRoot("KeepTraits")]
public class V2KeepTraitsSection : V2SpecialListSection
{
    [XmlElement("Trait")]
    public List<TraitDto> Trait = [];
}

/// <summary>The version-2 file model of the force-cull-list section of a special-list
/// preset.</summary>
[XmlRoot("ForceCullTraits")]
public class V2ForceCullTraitsSection : V2SpecialListSection
{
    [XmlElement("Trait")]
    public List<TraitDto> Trait = [];
}
