using System;
using System.Collections.Generic;

namespace ASM;

/// <summary>The preset subsystem's file access, kept behind an interface so the logic is
/// testable (swap the store; the production one works with the preset folders on disk).</summary>
public interface IPresetStore
{
    /// <summary>Paths of the <c>*.xml</c> files directly inside the folder (missing folder — empty).</summary>
    IEnumerable<string> GetFiles(string folder);

    /// <summary>Creates the folder (and parents) when missing.</summary>
    void EnsureFolder(string folder);

    string? ReadText(string path);

    void WriteText(string path, string text);

    void Delete(string path);

    DateTime GetLastWriteTime(string path);
}
