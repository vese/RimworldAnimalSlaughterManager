using Verse;

namespace ASM;

/// <summary>One integration's block inside the shared "Special rules" tab: an editor over
/// the kind's settings, created per kind settings window.</summary>
public interface ISpecialSettingsSection
{
    /// <summary>Draws the section; returns the height consumed.</summary>
    float Draw(float x, float y, float width, float height, IKindSettingsHost comp, ThingDef animalDef);

    /// <summary>Resets this section's slice of the kind settings (the tab's reset button).</summary>
    void Reset();
}
