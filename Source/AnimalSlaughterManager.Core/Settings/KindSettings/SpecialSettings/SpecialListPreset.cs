using System.Collections.Generic;
using System.Xml;

namespace ASM;

/// <summary>The special-settings list as the Settings-side model: the integration sections,
/// each integration owning its element's shape (see IKindSettingsProvider).</summary>
public class SpecialListPreset : BasePreset
{
    public List<XmlElement> Sections = [];
}
