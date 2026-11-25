namespace ASM;

/// <summary>
/// A settings object that knows which of its data takes part in kind presets and how.
/// Implementations fill/read their own slice of the <see cref="KindPresetDto"/> — the DTO itself
/// stays a plain data container.
/// </summary>
public interface IPresettable
{
    void Save(KindPresetDto dto);

    void Load(KindPresetDto dto);
}
