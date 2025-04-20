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
                [ConsoleKey.B] = CopyPattern
            };
        }

        private async Task Start()
        {
            Console.WriteLine("Старт воспроизведения");
            await _sequencer.StartAsync();
        }

        private async Task Stop()
        {
            Console.WriteLine("Стоп воспроизведения");
            await _sequencer.StopAsync();
        }

        private async Task Pause() => await _sequencer.PauseAsync();

        private async Task RecordNote()
        {
            int maxPos = _sequencer.CurrentPattern.Length;
            Console.Write($"Позиция (1-{maxPos}): ");
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

        private async Task ClearPattern() => await _sequencer.ClearPatternAsync();

        private async Task DumpState() => await _sequencer.DumpStateAsync();

        private async Task SaveProject()
        {
            Console.Write("Имя файла проекта: ");
            await _sequencer.SaveProjectAsync(Console.ReadLine() ?? "project.xml");
        }

        private async Task LoadProject()
        {
            Console.Write("Имя файла проекта: ");
            await _sequencer.LoadProjectAsync(Console.ReadLine() ?? "project.xml");
        }

        private async Task NextPattern() => await _sequencer.SwitchPatternAsync();

        private async Task CopyPattern() => await _sequencer.CopyPatternAsync();
    }
}