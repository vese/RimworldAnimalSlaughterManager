using System;

namespace ASM;

/// <summary>A lazily-computed value invalidated by any SettingsChanges raise: remembers the
/// global version it was computed at and recomputes on the next read when it moves. No
/// subscriptions — the owner needs no teardown, and no cache is ever stale.</summary>
public struct Cached<T> where T : class
{
    private T? value;
    private int version;

    public T Get(Func<T> compute)
    {
        if (value == null || version != SettingsChanges.Version)
        {
            value = compute();
            version = SettingsChanges.Version;
        }

        return value;
    }

    public void Invalidate() => version = -1;
}
