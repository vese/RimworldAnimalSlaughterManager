using Verse;

namespace ASM;

/// <summary>Converts a Settings preset model into the file form of a format version and back.
/// The implementation is resolved by the Settings model's type and the file's version; the
/// methods speak the base file form (the envelope).</summary>
public interface IPresetConverter<TModel> where TModel : BasePreset
{
    /// <summary>The model wrapped in the file form (the envelope) of this version.</summary>
    PresetFile ToFile(TModel model, PresetScope scope, string? bucket = null);

    /// <summary>The model from the file form; null when the file is not one of this model. A
    /// version-1 file arrives as a synthesized envelope (Version 1, the raw text as the
    /// content); the entry and the target def carry the apply's slicing context.</summary>
    TModel? ToModel(PresetFile file, PresetEntry entry, ThingDef? targetDef = null);
}
