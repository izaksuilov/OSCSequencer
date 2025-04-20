using Rug.Osc;
using System.Net;

namespace OSCSequencer
{
    public class ApplicationBootstrapper : IDisposable
    {
        private readonly Sequencer _sequencer;
        private readonly CommandManager _commandManager;
        private readonly OscSender _sender;
        private readonly CancellationTokenSource _cts = new();

        public ApplicationBootstrapper()
        {
            var settings = new OscSettings();
            _sender = new OscSender(IPAddress.Parse("127.0.0.1"), 9000);
            _sender.Connect();

            _sequencer = new Sequencer(
                initialBpm: 120,
                initialPatternLength: 16,
                oscSender: _sender,
                oscSettings: settings);

            _commandManager = new CommandManager(_sequencer);
        }

        public async Task RunAsync()
        {
            PrintHelp();
            await ProcessInputAsync();
        }

        private void PrintHelp()
        {
            Console.WriteLine("OSC Секвенсор запущен. Доступные команды:");
            Console.WriteLine("[S] Старт       [X] Стоп       [P] Пауза");
            Console.WriteLine("[R] Запись ноты [T] Темп       [L] Длина паттерна");
            Console.WriteLine("[C] Очистка     [D] Состояние  [F] Сохранить");
            Console.WriteLine("[G] Загрузить   [N] След. пат. [B] Копировать пат.");
            Console.WriteLine("[Q] Выход");
        }

        private async Task ProcessInputAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Q)
                        break;

                    if (_commandManager.Commands.TryGetValue(key, out var action))
                        await action.Invoke();
                }
                await Task.Delay(50);
            }
        }

        public void Dispose()
        {
            _sender.Dispose();
            _cts.Dispose();
            _sequencer.Dispose();
        }
    }
}