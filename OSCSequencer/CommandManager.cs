using OSCSequencer.SequencerData;

namespace OSCSequencer
{
    public class CommandManager
    {
        private readonly Sequencer _sequencer;

        public Dictionary<ConsoleKey, Func<Task>> Commands { get; }

        public CommandManager(Sequencer sequencer)
        {
            _sequencer = sequencer;
            Commands = new Dictionary<ConsoleKey, Func<Task>>
            {
                [ConsoleKey.S] = Start,
                [ConsoleKey.X] = Stop,
                [ConsoleKey.P] = Pause,
                [ConsoleKey.R] = RecordNotes,
                [ConsoleKey.Y] = RecordNote,
                [ConsoleKey.T] = SetTempo,
                [ConsoleKey.L] = SetPatternLength,
                [ConsoleKey.C] = ClearPattern,
                [ConsoleKey.D] = DumpState,
                [ConsoleKey.F] = SaveProject,
                [ConsoleKey.G] = LoadProject,
                [ConsoleKey.N] = NextPattern,
                [ConsoleKey.W] = AddPattern,
                [ConsoleKey.B] = CopyPattern,
                [ConsoleKey.M] = SwitchPlaybackMode,
                [ConsoleKey.V] = SwitchVisualization,
                [ConsoleKey.O] = ShowOscMessages,
                [ConsoleKey.A] = AddOscMessage,
                [ConsoleKey.E] = EditOscMessage,
                [ConsoleKey.U] = RemoveOscMessage,
                [ConsoleKey.J] = SaveOscMessagesToXml,
                [ConsoleKey.K] = LoadOscMessagesFromXml,
                [ConsoleKey.Z] = SendCustomOscMessage,
            };
        }

        public void PrintHelp()
        {
            Console.WriteLine("OSC Секвенсор запущен. Доступные команды:");

            Console.WriteLine();
            Console.WriteLine("[S] Старт  [X] Стоп  [P] Пауза");
            Console.WriteLine("[M] Режим воспроизведения");
            Console.WriteLine("[V] Режим визуализации");

            Console.WriteLine();
            Console.WriteLine("[G] Загрузить проект  [F] Сохранить проект");
            Console.WriteLine("[R] Запись нот        [Y] Запись одной ноты");
            Console.WriteLine("[T] Запись темпа");

            Console.WriteLine();
            Console.WriteLine("[C] Очистка паттерна  [D] Состояние паттерна");
            Console.WriteLine("[W] Создать паттерн   [B] Копировать паттерн");
            Console.WriteLine("[L] Длина паттерна    [N] След. паттерн");

            Console.WriteLine();
            Console.WriteLine("[O] Список OSC-сообщений");
            Console.WriteLine("[A] Добавить OSC-сообщение");
            Console.WriteLine("[E] Редактировать OSC-сообщение");
            Console.WriteLine("[U] Удалить OSC-сообщение");
            Console.WriteLine("[J] Сохранить OSC-сообщения");
            Console.WriteLine("[K] Загрузить OSC-сообщения");
            Console.WriteLine("[Z] Отправить произвольную OSC-команду");
        }

        #region Sequencer Commands

        private async Task Start()
        {
            Console.WriteLine($"Старт воспроизведения в режиме {_sequencer.PlaybackMode}");
            try
            {
                await _sequencer.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запуска: {ex.Message}");
            }
        }

        private async Task Stop()
        {
            Console.WriteLine("Стоп воспроизведения");
            try
            {
                await _sequencer.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка остановки: {ex.Message}");
            }
        }

        private async Task Pause()
        {
            if (_sequencer.IsPaused)
                Console.WriteLine("Продолжить");
            else
                Console.WriteLine("Пауза");

            try
            {
                await _sequencer.PauseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка паузы: {ex.Message}");
            }
        }

        private async Task RecordNotes()
        {
            Console.WriteLine("Запись нот");

            try
            {
                int patternLength = _sequencer.CurrentPattern.Length;
                Console.WriteLine($"Введите до {patternLength} нот через пробел:");
                string? notesLine = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(notesLine))
                {
                    Console.WriteLine("Ввод пустой. Операция отменена.");
                    return;
                }

                await _sequencer.RecordNotesAsync(notesLine);
                Console.WriteLine("Ноты записаны!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи: {ex.Message}");
            }
        }

        private async Task RecordNote()
        {
            Console.WriteLine("Запись ноты");

            try
            {
                int maxPos = _sequencer.CurrentPattern.Length;
                Console.Write($"Позиция в паттерне (1-{maxPos}): ");
                int pos = int.Parse(Console.ReadLine() ?? "1");

                Console.Write("Нота (0-127): ");
                int note = int.Parse(Console.ReadLine() ?? "60");

                await _sequencer.RecordNoteAsync(pos - 1, note);
                Console.WriteLine("Нота записана!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи: {ex.Message}");
            }
        }

        private async Task SetTempo()
        {
            Console.Write("Новый BPM: ");
            try
            {
                int bpm = int.Parse(Console.ReadLine() ?? "120");
                await _sequencer.SetTempoAsync(bpm);
            }
            catch (FormatException)
            {
                Console.WriteLine("Неверный формат BPM. Попробуйте снова.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка установки bpm: {ex.Message}");
            }
        }

        private async Task SetPatternLength()
        {
            Console.Write("Новая длина: ");
            try
            {
                int length = int.Parse(Console.ReadLine() ?? "16");
                await _sequencer.SetPatternLengthAsync(length);
            }
            catch (FormatException)
            {
                Console.WriteLine("Неверный формат длины. Попробуйте снова.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка установки длины: {ex.Message}");
            }
        }

        private async Task ClearPattern()
        {
            Console.WriteLine($"Очистка паттерна {_sequencer.Project.CurrentPatternIndex}");
            try
            {
                await _sequencer.ClearPatternAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка очистки паттерна: {ex.Message}");
            }
        }

        private async Task DumpState() => await _sequencer.DumpStateAsync();

        private async Task SaveProject()
        {
            Console.Write("Имя файла проекта: ");
            try
            {
                string filename = Console.ReadLine() ?? "project.xml";
                await _sequencer.SaveProjectAsync(filename);
                Console.WriteLine("Проект сохранен!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения проекта: {ex.Message}");
            }
        }

        private async Task LoadProject()
        {
            Console.Write("Имя файла проекта для загрузки: ");
            try
            {
                string filename = Console.ReadLine() ?? "project.xml";
                await _sequencer.LoadProjectAsync(filename);
                Console.WriteLine("Проект загружен!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки проекта: {ex.Message}");
            }
        }

        private async Task NextPattern()
        {
            try
            {
                await _sequencer.SwitchPatternAsync();
                Console.WriteLine($"Переключен паттерн {_sequencer.Project.CurrentPatternIndex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка переключения паттерна: {ex.Message}");
            }
        }

        private async Task AddPattern()
        {
            Console.Write("Длина нового паттерна: ");
            try
            {
                int length = int.Parse(Console.ReadLine() ?? "16");
                await _sequencer.AddPatternAsync(length);
                Console.WriteLine("Паттерн добавлен!");
            }
            catch (FormatException)
            {
                Console.WriteLine("Неверный формат длины. Попробуйте снова.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления паттерна: {ex.Message}");
            }
        }

        private async Task CopyPattern()
        {
            try
            {
                await _sequencer.CopyPatternAsync();
                Console.WriteLine($"Скопирован паттерн {_sequencer.Project.CurrentPatternIndex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка копирования паттерна: {ex.Message}");
            }
        }

        private async Task SwitchPlaybackMode()
        {
            try
            {
                await _sequencer.SwitchPlaybackMode();
                Console.WriteLine($"Выбран режим воспроизведения {_sequencer.PlaybackMode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выбора режима воспроизведения: {ex.Message}");
            }
        }

        private async Task SwitchVisualization()
        {
            try
            {
                _sequencer.SwitchVisualizeMode();
                Console.WriteLine($"Режим визуализации {(_sequencer.IsVisualizationEnabled ? "Вкл" : "Выкл")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выбора режима визуализации: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        #endregion

        #region OSC Commands

        private async Task ShowOscMessages()
        {
            Console.WriteLine("Список OSC-сообщений:");
            foreach (var kvp in _sequencer.OscSettings.Messages)
            {
                Console.WriteLine($"[{kvp.Key}] = \"{kvp.Value}\"");
            }
            await Task.CompletedTask;
        }

        private async Task AddOscMessage()
        {
            Console.Write("Введите ключ нового сообщения: ");
            string key = Console.ReadLine() ?? "";
            Console.Write("Введите значение OSC-адреса: ");
            string value = Console.ReadLine() ?? "";
            if (_sequencer.OscSettings.AddMessage(key, value))
                Console.WriteLine("Сообщение добавлено.");
            else
                Console.WriteLine("Ошибка: ключ уже существует.");
            await Task.CompletedTask;
        }

        private async Task EditOscMessage()
        {
            Console.Write("Введите существующий ключ сообщения: ");
            string oldKey = Console.ReadLine() ?? "";
            Console.Write("Введите новый ключ (или оставьте пустым для без изменений): ");
            string? newKey = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(newKey)) newKey = oldKey;
            Console.Write("Введите новое значение OSC-адреса: ");
            string newValue = Console.ReadLine() ?? "";
            if (_sequencer.OscSettings.EditMessage(oldKey, newKey, newValue))
                Console.WriteLine("Сообщение изменено.");
            else
                Console.WriteLine("Ошибка: ключ не найден.");
            await Task.CompletedTask;
        }

        private async Task RemoveOscMessage()
        {
            Console.Write("Введите ключ сообщения для удаления: ");
            string key = Console.ReadLine() ?? "";
            if (_sequencer.OscSettings.RemoveMessage(key))
                Console.WriteLine("Сообщение удалено.");
            else
                Console.WriteLine("Ошибка: ключ не найден.");
            await Task.CompletedTask;
        }

        private async Task SaveOscMessagesToXml()
        {
            Console.Write("Имя файла для сохранения OSC-сообщений: ");
            string filename = Console.ReadLine() ?? "osc_messages.xml";
            try
            {
                _sequencer.OscSettings.SaveToXml(filename);
                Console.WriteLine("OSC-сообщения сохранены.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private async Task LoadOscMessagesFromXml()
        {
            Console.Write("Имя файла для загрузки OSC-сообщений: ");
            string filename = Console.ReadLine() ?? "osc_messages.xml";
            try
            {
                _sequencer.OscSettings.LoadFromXml(filename);
                Console.WriteLine("OSC-сообщения загружены.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private async Task SendCustomOscMessage()
        {
            Console.Write("Введите OSC-адрес: ");
            string? address = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(address))
            {
                Console.WriteLine("OSC-адрес не может быть пустым.");
                return;
            }

            Console.Write("Введите параметры через пробел (можно оставить пустым): ");
            string? argsLine = Console.ReadLine();
            object[] args = Array.Empty<object>();
            if (!string.IsNullOrWhiteSpace(argsLine))
            {
                args = argsLine
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s =>
                    {
                        // Попробуем преобразовать к int, double, иначе оставим строкой
                        if (int.TryParse(s, out int i)) return (object)i;
                        if (double.TryParse(s, out double d)) return (object)d;
                        return s;
                    })
                    .ToArray();
            }

            try
            {
                _sequencer.SendOsc(address, args);
                Console.WriteLine("OSC-команда отправлена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки OSC: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        #endregion
    }
}