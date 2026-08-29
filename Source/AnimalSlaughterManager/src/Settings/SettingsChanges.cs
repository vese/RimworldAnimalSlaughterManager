using System;

namespace ASM;

public static class SettingsChanges
{
    public static event Action? Changed;

    public static void Raise() => Changed?.Invoke();
}
