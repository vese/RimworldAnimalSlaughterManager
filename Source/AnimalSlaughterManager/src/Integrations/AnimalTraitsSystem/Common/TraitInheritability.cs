using Verse;

namespace ASM;

/// <summary>Filter a trait target by the trait's inheritability.</summary>
public enum TraitInheritability
{
    Both = 0,
    Inheritable = 1,
    NonInheritable = 2
}

public static class TraitInheritabilityExtensions
{
    public static TaggedString Translate(this TraitInheritability value) => value switch
    {
        TraitInheritability.Both => ASMKeys.InhBoth.Translate(),
        TraitInheritability.Inheritable => ASMKeys.InhInheritable.Translate(),
        TraitInheritability.NonInheritable => ASMKeys.InhNonInheritable.Translate(),
        _ => throw new System.NotImplementedException(),
    };
}
