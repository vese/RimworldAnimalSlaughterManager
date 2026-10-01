using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>
/// Two-button trait picker for condition lists: each trait row has a green ✓ ("has") and a
/// red ✗ ("missing") checkbox; "Add selected" hands the (trait, flag) pairs over.
/// </summary>
public class Dialog_TraitFlagPicker : Dialog_TraitTable
{
    private const float ChecksW = 2f * UIConstants.IconSize;
    private const float CheckSize = 18f;

    private readonly Action<List<(HediffDef def, bool has)>> onPicked;
    private readonly Dictionary<HediffDef, bool> selFlags = new Dictionary<HediffDef, bool>();

    public Dialog_TraitFlagPicker(Action<List<(HediffDef, bool)>> onPicked)
    {
        this.onPicked = onPicked;
    }

    protected override float LeadingColumnWidth => ChecksW;

    protected override void DrawLeadingHeaderIcons(Rect r)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Tiny;
        GUI.color = Color.green;
        Widgets.Label(new Rect(r.x, r.y, UIConstants.IconSize, UIConstants.IconSize), "✓");
        GUI.color = Color.red;
        Widgets.Label(new Rect(r.x + UIConstants.IconSize, r.y, UIConstants.IconSize, UIConstants.IconSize), "✗");
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    protected override void DrawRow(Rect row, Row r, List<Col> vis)
    {
        DrawChecks(row, r);
        base.DrawRow(new Rect(row.x + ChecksW, row.y, row.width - ChecksW, row.height), r, vis);
    }

    protected override void Confirm() => onPicked(selected.Select(d => (d, selFlags[d])).ToList());

    private void DrawChecks(Rect row, Row r)
    {
        // Two paint zones: left = has (green), right = missing (red). Drag across to paint.
        Rect hasZone = new Rect(row.x, row.y, UIConstants.IconSize, row.height);
        Rect missZone = new Rect(row.x + UIConstants.IconSize, row.y, UIConstants.IconSize, row.height);
        bool isHas = selected.Contains(r.def) && selFlags.ContainsKey(r.def) && selFlags[r.def];
        bool isMissing = selected.Contains(r.def) && selFlags.ContainsKey(r.def) && !selFlags[r.def];

        if (Mouse.IsOver(hasZone) || Mouse.IsOver(missZone))
        {
            bool overHas = Mouse.IsOver(hasZone);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                paintMode = true; paintValue = overHas;
                selected.Add(r.def); selFlags[r.def] = overHas; Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDrag && paintMode)
            {
                if (overHas != paintValue) paintValue = overHas;
                selected.Add(r.def); selFlags[r.def] = overHas;
            }
        }

        // Vanilla CheckboxDraw — no color tint, native rendering.
        float cbY = row.y + (row.height - CheckSize) / 2f;
        Rect hasRect = new Rect(row.x + UIConstants.TextPaddingY, cbY, CheckSize, CheckSize);
        Rect missRect = new Rect(row.x + UIConstants.IconSize, cbY, CheckSize, CheckSize);
        TooltipHandler.TipRegion(hasRect, ASMKeys.CondHasTip.Translate());
        TooltipHandler.TipRegion(missRect, ASMKeys.CondMissingTip.Translate());
        Widgets.CheckboxDraw(hasRect.x, hasRect.y, isHas, !isHas, CheckSize);
        Widgets.CheckboxDraw(missRect.x, missRect.y, isMissing, !isMissing, CheckSize);
    }
}
