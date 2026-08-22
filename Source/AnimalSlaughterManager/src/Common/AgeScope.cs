using Verse;

namespace ASM;

/// <summary>Which age group a trait target applies to.</summary>
public enum AgeScope
{
    Both = 0,
    Adult = 1,
    Young = 2
}

public static class AgeScopeExtensions
{
    public static TaggedString Translate(this AgeScope value) => value switch
    {
        AgeScope.Both => ASMKeys.AgeBoth.Translate(),
        AgeScope.Adult => ASMKeys.AgeAdult.Translate(),
        AgeScope.Young => ASMKeys.AgeYoung.Translate(),
        _ => throw new System.NotImplementedException(),
    };
}