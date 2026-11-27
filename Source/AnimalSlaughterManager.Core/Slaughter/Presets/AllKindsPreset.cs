using System.Collections.Generic;

namespace ASM;

/// <summary>The map's full preset: every customized kind's slice. Built and read by
/// ASM_MapComp.ToPreset/FromPreset; the file form is Presets' business.</summary>
public class AllKindsPreset : BasePreset
{
    public List<KindPreset> Kinds { get; set; } = [];
}
