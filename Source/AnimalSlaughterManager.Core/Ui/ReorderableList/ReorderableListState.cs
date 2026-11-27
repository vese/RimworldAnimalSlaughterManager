using UnityEngine;

namespace ASM;

/// <summary>Per-list UI state: the scroll position and the cached reorder group id.</summary>
public class ReorderableListState
{
    public Vector2 scroll;
    public int group;
}
