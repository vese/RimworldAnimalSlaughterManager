using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-2 file model of the all-kinds preset: every configured kind's
/// slice.</summary>
public class V2AllKindsPreset
{
    [XmlElement("Kind")]
    public List<V2KindPreset> Kinds = [];
}
