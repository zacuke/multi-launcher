using System.Diagnostics;
using System.Runtime.InteropServices;

namespace multi_launcher
{
    class Program
    {

        readonly static ConsoleEventDelegate closeHandler = new (CloseHandler);
        readonly static List<Process> processList = [];
        readonly static CancellationTokenSource cts = new();

        delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        static async Task Main(string[] args)
        {            
 
            //setup handler for when app closes
            SetConsoleCtrlHandler(closeHandler, true);

            //setup handler for when ctrl-c is pressed
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelHandler);

            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    var spaPath = Path.GetFullPath(Path.Combine("..", "..", "soad", "trading-dashboard", "build"));
                    string bindUrl = "http://0.0.0.0:3000";
                    services.AddSingleton<IHostedService>(sp => new SpaLauncher(spaPath, bindUrl));
                    services.AddSingleton<IHostLifetime, DisableCtrlCLifeTime>();


                });

            var soadFolder = "c:\\src\\soad";
            processList.Add(ProcessLauncher.ExecuteLaunchProcess("soad API",
                //"C:\\Users\\garth\\AppData\\Local\\Programs\\Python\\Python313\\python.exe",
                //"main.py --mode api",
                "cmd",
                "/c python main.py --mode api",
                Path.GetFullPath(soadFolder),
                cts.Token));

            var host = hostBuilder.Build();
            var hostTask = host.RunAsync(cts.Token);
            await hostTask;
        }

        static bool CloseHandler(int eventType)
        {
            //windows gives you 5 seconds
            if (eventType == 2)
            {
                KillAllProcesses();
            }
            return false;
        }

        static void CancelHandler(object? sender, ConsoleCancelEventArgs args)
        {
            KillAllProcesses();
            cts.Cancel();
            args.Cancel = true;
        }

        static void KillAllProcesses()
        {
            foreach (var process in processList)
            {
                var childProcesses = process.GetChildProcesses();
                foreach (var i in childProcesses)
                {
                    i.Kill();
                }
                process.Kill();
            }
        }
    }
}

