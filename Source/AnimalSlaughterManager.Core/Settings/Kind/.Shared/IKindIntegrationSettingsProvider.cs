using System.Xml;

namespace ASM;

public interface IKindIntegrationSettingsProvider
{
    string KindSettingsKey { get; }

    IKindIntegrationSettings CreateKindSettings();
}
