namespace ASM;

/// <summary>A force-cull target: animals of the kind carrying it are culled regardless of
/// count and limits (keep-reservation still wins at intersections).</summary>
public class ForceCullRule : SpecialRule
{
    public override SpecialRule Copy() => new ForceCullRule
    {
        target = target,
        ageScope = ageScope,
        genderScope = genderScope,
        inheritMode = inheritMode
    };
}
