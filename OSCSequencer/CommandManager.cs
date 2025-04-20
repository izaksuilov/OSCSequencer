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
                [ConsoleKey.R] = RecordNote,
                [ConsoleKey.T] = SetTempo,
                [ConsoleKey.L] = SetPatternLength,
                [ConsoleKey.C] = ClearPattern,
                [ConsoleKey.D] = DumpState,
                [ConsoleKey.F] = SaveProject,
                [ConsoleKey.G] = LoadProject,
                [ConsoleKey.N] = NextPattern,
                [ConsoleKey.B] = CopyPattern,
                [ConsoleKey.M] = SwitchPlaybackMode,
                [ConsoleKey.V] = SwitchVisualization,
            };
        }

        public void PrintHelp()
        {
            Console.WriteLine("OSC Секвенсор запущен. Доступные команды:");
            Console.WriteLine("[M] Режим воспроизведения [V] Режим визуализации");
            Console.WriteLine("[S] Старт       [X] Стоп       [P] Пауза");
            Console.WriteLine("[R] Запись ноты [T] Темп       [L] Длина паттерна");
            Console.WriteLine("[C] Очистка     [D] Состояние  [F] Сохранить");
            Console.WriteLine("[G] Загрузить   [N] След. пат. [B] Копировать пат.");
            Console.WriteLine("[Q] Выход");
        }

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
                await _sequencer.SwitchPlaybackMode();
                Console.WriteLine($"Режим визуализации {(_sequencer.IsVisualizationEnabled ? "Вкл" : "Выкл")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выбора режима визуализации: {ex.Message}");
            }
        }
    }
}