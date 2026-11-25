using System.Xml.Serialization;

namespace ASM;

[XmlRoot("TrainingGeneral")]
public class TrainingGeneralRulePreset
{
    public string type = nameof(TrainingGeneralType.None);
}
