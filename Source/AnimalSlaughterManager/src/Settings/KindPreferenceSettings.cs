using System;
using Verse;

namespace ASM;

/// <summary>Per-kind preference settings (Priorities tab). Save keys: "malePref", …</summary>
public class KindPreferenceSettings : PreferenceSettings
{
    [Obsolete]
    public SlaughterPreference defaultAgePref = SlaughterPreference.OldestFirst;

    protected override string MalePrefKey => "malePref";
    protected override string FemalePrefKey => "femalePref";
    protected override string MaleYoungPrefKey => "maleYoungPref";
    protected override string FemaleYoungPrefKey => "femaleYoungPref";

    public override bool Customized => base.Customized || defaultAgePref is SlaughterPreference.YoungestFirst;

    public override void Reset()
    {
        // TODO: use global preferences
        base.Reset();
        defaultAgePref = SlaughterPreference.OldestFirst;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref defaultAgePref, "defaultAgePref", SlaughterPreference.OldestFirst);
    }
}
