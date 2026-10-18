using System;
using System.Collections.Generic;
using Verse;

namespace ASM;

/// <summary>Supplies a dropdown with data: the selectable values, the current one, how to label
/// them and how to apply a choice. The dropdown itself decides how to render them.</summary>
public interface IDropdownController
{
    /// <summary>Width the dropdown occupies in its row.</summary>
    float Width { get; }

    /// <summary>Renders through the shared <see cref="Dropdown"/> widget.</summary>
    void Draw(float x, float y);
}

/// <summary>Value-typed dropdown data: the rule fills it in, the widget draws it.</summary>
public sealed class DropdownController<T> : IDropdownController where T : notnull
{
    public float Width { get; set; } = UIConstants.ButtonMinWidth;
    public T Current { get; set; } = default!;
    public IEnumerable<T> Values { get; set; } = [];
    public Func<T, TaggedString> LabelOf { get; set; } = _ => "";
    public Action<T> SetValue { get; set; } = _ => { };

    void IDropdownController.Draw(float x, float y) => Dropdown.Draw(x, y, this);
}
