namespace ASM;

/// <summary>
/// A settings object that knows which of its data takes part in kind presets and how.
/// Implementations fill/read their own slice of the <see cref="KindDto"/> — the DTO itself
/// stays a plain data container.
/// </summary>
public interface IPresettable
{
    void Save(KindDto dto);

    void Load(KindDto dto);
}

/// <summary>
/// A rule set that knows its own List-preset slot: the presets subfolder, and how to
/// save/read its rules from the matching <see cref="KindDto"/> field.
/// </summary>
public interface IPresettableRuleSet
{
    string PresetsFolder { get; }

    void Save(KindDto dto);

    /// <summary>True when the DTO carries this rule set's slice.</summary>
    bool Load(KindDto dto);
}
