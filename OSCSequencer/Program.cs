using OSCSequencer;

namespace OSCSecvensor
{
    public class Program
    {
        private static ApplicationBootstrapper? _app;

        static async Task Main(string[] args)
        {
            _app = new ApplicationBootstrapper(args);
            await _app.RunAsync();
        }
    }
}