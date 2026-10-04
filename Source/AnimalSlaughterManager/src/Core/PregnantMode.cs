namespace ASM;

/// <summary>How pregnant/egg-carrying females are treated by the slaughter threshold.</summary>
public enum PregnantMode
{
    /// <summary>Never slaughter them; they are not counted toward the threshold.</summary>
    Never,
    /// <summary>Counted toward the threshold, but a pregnant female selected for slaughter is
    /// deferred (kept alive) until she gives birth, and does not take a kept slot.</summary>
    Defer,
    /// <summary>Slaughtered normally and counted toward the threshold.</summary>
    Always
}
