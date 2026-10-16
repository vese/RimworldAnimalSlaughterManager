using System;
using UnityEngine;
using Verse;

namespace ASM;

public interface IKindSlaughterSettingsDialogTab
{
    TaggedString Name { get; }
    void DoWindowContents(Rect inRect, IKindSettingsHost comp, ThingDef animalDef, KindSettings settings, Action<float, float, float, float> drawTabBar);
}
