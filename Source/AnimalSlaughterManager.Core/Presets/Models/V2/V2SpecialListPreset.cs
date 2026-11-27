using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASM;

/// <summary>The version-2 file model of a special-list preset: the integration settings under
/// their provider keys (V2SpecialSettingsPreset items).</summary>
[XmlRoot("SpecialList")]
public class V2SpecialListPreset
{
    public List<V2SpecialSettingsPreset> Settings = [];
}
