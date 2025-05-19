using System.Xml.Serialization;

namespace OSCSequencer.Osc
{
    public class OscSettings
    {
        private Dictionary<string, string> _messages = new()
        {
            ["noteon"] = "/synth/noteon",
            ["noteoff"] = "/synth/noteoff",
            ["tempo"] = "/global/tempo",
            ["pattern"] = "/global/pattern",
            ["pattern_noteon"] = "/pattern{0}/noteon",
            ["pattern_noteoff"] = "/pattern{0}/noteoff",
        };

        public Dictionary<string, string> Messages
        {
            get => new(_messages);
            private set => _messages = value;
        }

        // Добавление сообщения
        public bool AddMessage(string key, string value)
        {
            if (_messages.ContainsKey(key))
                return false;

            _messages[key] = value;
            return true;
        }

        // Удаление сообщения
        public bool RemoveMessage(string key) => _messages.Remove(key);

        // Редактирование сообщения
        public bool EditMessage(string oldKey, string newKey, string newValue)
        {
            if (!_messages.ContainsKey(oldKey))
                return false;

            if (oldKey != newKey)
            {
                if (_messages.ContainsKey(newKey))
                    return false;

                _messages.Remove(oldKey);
            }

            _messages[newKey] = newValue;
            return true;
        }

        // Сохранение в XML
        public void SaveToXml(string filePath)
        {
            try
            {
                var wrapper = new MessageWrapper();
                wrapper.Messages.AddRange(_messages.Select(kvp =>
                    new OscMessage { Key = kvp.Key, Value = kvp.Value }));

                var serializer = new XmlSerializer(typeof(MessageWrapper));
                using var writer = new StreamWriter(filePath);
                serializer.Serialize(writer, wrapper);
            }
            catch (Exception ex)
            {
                // Обработка ошибок десериализации
                Console.WriteLine("Ошибка сохранения XML: " + ex.Message);
            }
        }

        // Загрузка из XML
        public void LoadFromXml(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("XML file not found");

                var serializer = new XmlSerializer(typeof(MessageWrapper));
                using var reader = new StreamReader(filePath);
                var wrapper = serializer.Deserialize(reader) as MessageWrapper;

                if (wrapper is not null)
                {
                    _messages = wrapper.Messages
                        .ToDictionary(m => m.Key, m => m.Value);
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок десериализации
                Console.WriteLine("Ошибка загрузки XML: " + ex.Message);
            }
        }
    }
}