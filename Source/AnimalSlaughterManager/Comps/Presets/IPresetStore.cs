using System;
using System.Collections.Generic;
using System.IO;

namespace ASM;

/// <summary>The preset subsystem's file access, kept behind an interface so the logic is
/// testable (swap the store; the production one works with the preset folders on disk).</summary>
public interface IPresetStore
{
    /// <summary>Paths of the <c>*.xml</c> files directly inside the folder (missing folder — empty).</summary>
    IEnumerable<string> Files(string folder);

    /// <summary>Creates the folder (and parents) when missing.</summary>
    void EnsureFolder(string folder);

    string? ReadText(string path);

    void WriteText(string path, string text);

    void Delete(string path);

    DateTime LastWrite(string path);
}

public sealed class FilePresetStore : IPresetStore
{
    public IEnumerable<string> Files(string folder) =>
        Directory.Exists(folder) ? Directory.GetFiles(folder, "*.xml", SearchOption.TopDirectoryOnly) : Array.Empty<string>();

    public void EnsureFolder(string folder) => Directory.CreateDirectory(folder);

    public string? ReadText(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

    public void WriteText(string path, string text) => File.WriteAllText(path, text);

    public void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public DateTime LastWrite(string path) => File.GetLastWriteTime(path);
}
