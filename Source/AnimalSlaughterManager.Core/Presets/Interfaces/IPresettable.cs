namespace ASM;

/// <summary>
/// A settings object that knows its own preset model: it builds the model from its state and
/// reads its state back from it — the model itself stays a plain data container.
/// </summary>
public interface IPresettable<TPreset>
{
    TPreset ToPreset();

    void FromPreset(TPreset preset);
}
