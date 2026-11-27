using System.Xml.Serialization;
using System.Collections.Generic;

namespace ASM;

/// <summary>The version-2 file model of a special-list preset: the owning settings' own
/// sections (typed — see V2SpecialListSection).</summary>
[XmlRoot("SpecialList")]
public class V2SpecialListPreset
{
    public List<V2SpecialListSection> Sections = [];
}
