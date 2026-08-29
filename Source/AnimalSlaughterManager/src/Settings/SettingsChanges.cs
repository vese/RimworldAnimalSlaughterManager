using System;

namespace ASM;

public class SettingsChanges
{
    public event Action? Changed;

    public void Raise() => Changed?.Invoke();
}
