using System;
using System.Collections.Generic;
using System.IO;

namespace ASM;

/// <summary>The production store: the preset folders on disk.</summary>
public sealed class FilePresetStore : IPresetStore
{
    public IEnumerable<string> GetFiles(string folder) =>
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

    public DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);
}
