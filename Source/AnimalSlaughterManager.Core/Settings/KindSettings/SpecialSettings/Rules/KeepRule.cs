using Verse;

namespace ASM;

/// <summary>A breeding ("keep") target: keep a number of animals of the kind carrying the
/// target. When the integration marks the target inheritable, the selection biases toward
/// keeping a breeding pair (the engine side decides).</summary>
public class KeepRule : SpecialRule
{
    public int keepCount = 1;

    public override SpecialRule Copy() => new KeepRule
    {
        target = target,
        keepCount = keepCount,
        ageScope = ageScope,
        genderScope = genderScope,
        inheritMode = inheritMode
    };

    public void SetKeepCount(int value)
    {
        keepCount = value;
        SettingsChangeTracker.Raise();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref keepCount, "keepCount", 1);
    }
}
