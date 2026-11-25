namespace ASM;

/// <summary>A preset DTO carrying its format version: the writer stamps the latest one, the
/// reader refuses versions above its own and migrates older ones.</summary>
public interface IVersionedPresetDto
{
    int version { get; set; }
}
