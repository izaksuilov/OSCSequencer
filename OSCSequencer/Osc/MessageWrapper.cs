using System.Xml.Serialization;

namespace OSCSequencer.Osc
{
    // Класс-обертка для сериализации
    [XmlRoot("OscSettings")]
    public class MessageWrapper
    {
        [XmlElement("Message")]
        public List<OscMessage> Messages { get; } = new();
    }
}