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
            await _sequencer.StartAsync();
        }

        private async Task Stop()
        {
            Console.WriteLine("Стоп воспроизведения");
            await _sequencer.StopAsync();
        }

        private async Task Pause()
        {
            if (_sequencer.IsPaused)
                Console.WriteLine("Продолжить");
            else
                Console.WriteLine("Пауза");

            await _sequencer.PauseAsync();
        }

        private async Task RecordNote()
        {
            Console.WriteLine("Запись ноты");

            int maxPos = _sequencer.CurrentPattern.Length;
            Console.Write($"Позиция в паттерне (1-{maxPos}): ");
            int pos = int.Parse(Console.ReadLine() ?? "1");

            Console.Write("Нота (0-127): ");
            int note = int.Parse(Console.ReadLine() ?? "60");

            await _sequencer.RecordNoteAsync(pos - 1, note);

            Console.WriteLine("Нота записана!");
        }

        private async Task SetTempo()
        {
            Console.Write("Новый BPM: ");
            await _sequencer.SetTempoAsync(int.Parse(Console.ReadLine() ?? "120"));
        }

        private async Task SetPatternLength()
        {
            Console.Write("Новая длина: ");
            await _sequencer.SetPatternLengthAsync(int.Parse(Console.ReadLine() ?? "16"));
        }

        private async Task ClearPattern()
        {
            Console.WriteLine($"Очистка паттерна {_sequencer.Project.CurrentPatternIndex}");
            await _sequencer.ClearPatternAsync();
        }

        private async Task DumpState() => await _sequencer.DumpStateAsync();

        private async Task SaveProject()
        {
            Console.Write("Имя файла проекта: ");
            await _sequencer.SaveProjectAsync(Console.ReadLine() ?? "project.xml");
            Console.WriteLine("Проект сохранен!");
        }

        private async Task LoadProject()
        {
            Console.Write("Имя файла проекта для загрузки: ");
            await _sequencer.LoadProjectAsync(Console.ReadLine() ?? "project.xml");
            Console.WriteLine("Проект загружен!");
        }

        private async Task NextPattern() => await _sequencer.SwitchPatternAsync();

        private async Task CopyPattern()
        {
            await _sequencer.CopyPatternAsync();
            Console.WriteLine($"Скопирован паттерн {_sequencer.Project.CurrentPatternIndex}");
        }

        private async Task SwitchPlaybackMode()
        {
            await _sequencer.SwitchPlaybackMode();
            Console.WriteLine($"Выбран режим воспроизведения {_sequencer.PlaybackMode}");
        }

        private async Task SwitchVisualization()
        {
            await _sequencer.SwitchPlaybackMode();
            Console.WriteLine($"Режим визуализации {(_sequencer.IsVisualizationEnabled ? "Вкл" : "Выкл")}");
        }
    }
}