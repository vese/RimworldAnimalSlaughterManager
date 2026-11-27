using Verse;

namespace ASM;

/// <summary>
/// Self-registration of the core's own preset buckets: RimWorld runs every
/// [StaticConstructorOnStartup] type, and the condition buckets join the listing through the
/// same contract the integrations use.
/// </summary>
[StaticConstructorOnStartup]
public static class PresetsRegistration
{
    static PresetsRegistration()
    {
        IntegrationServices.Add<IPresetBucketProvider>(new PriorityPresetBuckets());
    }
}
