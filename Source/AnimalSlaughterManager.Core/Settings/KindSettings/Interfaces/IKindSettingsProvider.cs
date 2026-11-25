using System.Xml;

namespace ASM;

public interface IKindSettingsProvider
{
    string Key { get; }

    SpecialSettingsPreset Create();
}
