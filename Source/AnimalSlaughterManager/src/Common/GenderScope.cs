using Verse;

namespace ASM;

/// <summary>Which sex a trait target applies to.</summary>
public enum GenderScope
{
    Any = 0,
    Male = 1,
    Female = 2
}

public static class GenderScopeExtensions
{
    public static TaggedString Translate(this GenderScope value) => value switch
    {
        GenderScope.Any => ASMKeys.GenderAny.Translate(),
        GenderScope.Male => ASMKeys.GenderMale.Translate(),
        GenderScope.Female => ASMKeys.GenderFemale.Translate(),
        _ => throw new System.NotImplementedException(),
    };
}
