using System;

namespace ASM;

public static class SettingsChanges
{
    public static event Action? Changed;

    /// <summary>Moves on every change. Caches remember the version they were computed at and
    /// recompute lazily when it moves — the universal invalidation for any settings cache,
    /// without per-owner subscriptions.</summary>
    public static int Version { get; private set; }

    public static void Raise()
    {
        Version++;
        Changed?.Invoke();
    }
}
