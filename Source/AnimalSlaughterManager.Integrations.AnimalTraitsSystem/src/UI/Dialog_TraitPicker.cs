using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ASM;

/// <summary>Single-select trait picker: pick traits, "Add selected" hands their defs over.</summary>
public class Dialog_TraitPicker : Dialog_TraitTable
{
    private const float CheckW = 30f;
    private const float CheckSize = 24f;

    private readonly Action<List<HediffDef>> onPicked;

    public Dialog_TraitPicker(Action<List<HediffDef>> onPicked)
    {
        this.onPicked = onPicked;
    }

    protected override float LeadingColumnWidth => CheckW;

    protected override void DrawRow(Rect row, Row r, List<Col> vis)
    {
        DrawChecks(row, r);
        base.DrawRow(new Rect(row.x + CheckW, row.y, row.width - CheckW, row.height), r, vis);
    }

    protected override void Confirm() => onPicked(selected.ToList());

    private void DrawChecks(Rect row, Row r)
    {
        // Paint selection: click + drag across the checkbox/name zone toggles rows together.
        Rect paintZone = new Rect(row.x, row.y, CheckW + NameW, row.height);
        bool value = selected.Contains(r.def);
        if (Mouse.IsOver(paintZone))
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                paintMode = true;
                paintValue = !value;
                value = paintValue;
                SetSel(r.def, value);
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDrag && paintMode)
            {
                if (value != paintValue) { value = paintValue; SetSel(r.def, value); }
            }
        }

        // Draw-only checkbox: ALL input is handled by the paint logic above. Using Widgets.Checkbox
        // here would double-toggle a single click (its own click handler reverts the paint toggle).
        Rect cb = new Rect(row.x + UIConstants.GapX - UIConstants.TextPaddingY, row.y + (row.height - CheckSize) / 2f, CheckSize, CheckSize);
        Widgets.CheckboxDraw(cb.x, cb.y, value, false, CheckSize);
    }

    private void SetSel(HediffDef d, bool on)
    {
        if (on) selected.Add(d); else selected.Remove(d);
    }
}
