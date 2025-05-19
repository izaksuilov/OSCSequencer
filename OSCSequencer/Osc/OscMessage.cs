using System.Xml.Serialization;

namespace OSCSequencer.Osc
{
    // Класс для хранения пары ключ-значение
    public class OscMessage
    {
        [XmlAttribute("key")]
        public string Key { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}