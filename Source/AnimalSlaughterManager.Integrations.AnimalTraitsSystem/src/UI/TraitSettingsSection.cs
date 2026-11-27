using System;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>The ATS section of the shared "Special rules" tab: the keep and force-cull lists.</summary>
public sealed class TraitSettingsSection(KindTraitsSettings traitsSettings) : ISpecialSettingsSection
{
    private const int RuleSetsCount = 2;
    private const float RuleSetsGapYSum = (RuleSetsCount - 1) * 2 * UIConstants.GapY;

    private readonly ITraitRulesListSection<TraitProtectRule> protectEditor = traitsSettings.protectRuleSet.GetEditor();
    private readonly ITraitRulesListSection<TraitRule> forceCullEditor = traitsSettings.forceCullRuleSet.GetEditor();
    private ReorderableListState protectEditorState = new();
    private ReorderableListState forceCullEditorState = new();

    public void Reset()
    {
        traitsSettings.Reset();
    }

    public float Draw(float x, float y, float width, float height, ISettingsProvider comp, ThingDef animalDef)
    {
        var listHeight = MathF.Max(UIConstants.ListMinHeight, (height - RuleSetsGapYSum) / RuleSetsCount);
        var top = y;

        top += protectEditor.Draw(x, top, width, listHeight, ref protectEditorState, comp, animalDef);

        top += UIConstants.GapY;

        var color = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.25f);
        Widgets.DrawLineHorizontal(x, top, width);
        GUI.color = color;

        top += UIConstants.GapY;

        top += forceCullEditor.Draw(x, top, width, listHeight, ref forceCullEditorState, comp, animalDef);

        return top - y;
    }
}
