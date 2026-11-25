using System.Linq;

namespace ASM;

/// <summary>Shared def lookup of the rule presets: the def's type is named by the owning side.</summary>
public static class RuleDtoDefs
{
    public static Verse.Def? ByName(System.Type defType, string? defName) =>
        defName == null ? null :
            Verse.GenDefDatabase.GetAllDefsInDatabaseForDef(defType).FirstOrDefault(d => d.defName == defName);
}
