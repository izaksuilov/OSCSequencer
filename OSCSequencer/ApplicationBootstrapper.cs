using OSCSequencer.Osc;
using OSCSequencer.SequencerData;
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

        public ApplicationBootstrapper(string[]? args = null)
        {
            string ipString = "127.0.0.1";
            int port = 9000;

            if (args is not null)
            {
                if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                    ipString = args[0];

                if (args.Length > 1 && int.TryParse(args[1], out int parsedPort))
                    port = parsedPort;
            }

            _sender = new OscSender(IPAddress.Parse(ipString), port);
            _sender.Connect();

            OscSettings settings = new();

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
            _commandManager.PrintHelp();
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