using System.Xml;

namespace ASM;

public interface IKindSettingsProvider
{
    string Key { get; }

    /// <summary>The kind the settings belong to — integrations that migrate their legacy data
    /// into the core storage need the owner.</summary>
    IKindSettings Create(KindSettings owner);
}
