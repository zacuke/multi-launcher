using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using multi_launcher.Launchers;
using multi_launcher.Platforms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace multi_launcher
{
    class Program
    {

        readonly static CancellationTokenSource cts = new();
        readonly static IPlatform platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new WindowsImpl(CloseHandler)
            : new LinuxImpl();

        static async Task Main(string[] args)
        {

            if (args.Length == 0)
            {
                //setup handler for when app closes
                platform.MySetConsoleCtrlHandler();

                //setup handler for when ctrl-c is pressed
                platform.HandleCtrlC(platform.KillAllProcesses, cts);

                var config = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", false)
                        .Build();

                var multiLauncherConfig = config.GetSection("MultiLauncher").Get<MultiLauncherConfig.MultiLauncher>()
                    ?? throw new Exception("Unable to read configuration");

                if (multiLauncherConfig.Processes.Count == 0 && multiLauncherConfig.SpaApps.Count == 0)
                {
                    Console.WriteLine("No processes or spa apps found in config. Exiting.");
                    Environment.Exit(0);
                };

                if (multiLauncherConfig.SpaApps.Count == 0)
                    Console.WriteLine("No spa apps found in config");

                if (multiLauncherConfig.Processes.Count == 0)
                    Console.WriteLine("No processes found in config");

                var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {

                    foreach (var spaApp in multiLauncherConfig.SpaApps)
                    {
                        var spaPath = platform.IsWindows() ? spaApp.WindowsPath : spaApp.LinuxPath;
                        services.AddSingleton<IHostedService>(sp => new SpaLauncher(
                            spaPath, 
                            spaApp.BindUrls, 
                            spaApp.SpaResponseContentType, 
                            spaApp.IndexHtml,
                            spaApp.SpaResponseHeaders)
                        );
                    }

                    services.AddSingleton<IHostLifetime, DisableCtrlCLifeTime>();

                });

                foreach (var processConfig in multiLauncherConfig.Processes)
                {
                    var platformConfig = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? processConfig.Windows
                        : processConfig.Linux;

                    Console.WriteLine($"Launching Process: {processConfig.Name}, Path: {platformConfig.Path}");

                    platform.LaunchProcess(
                         processConfig.Name,
                         platformConfig.Cmd,
                         platformConfig.Args,
                         Path.GetFullPath(platformConfig.Path),
                         processConfig.ProcessEnvironment);
                }
      
                var host = hostBuilder.Build();
                var hostTask = host.RunAsync(cts.Token);

                Console.WriteLine("Multi Launcher started. Press Ctrl+C to shut down.");
                await hostTask;
            }
            else
            {
                //https://stackoverflow.com/a/29274238/3594197
                var id = (uint)int.Parse(args[0]);
                platform.GenerateCtrlCEvent(id);
            }
        }

        static bool CloseHandler(int eventType)
        {
            //windows gives you 5 seconds
            if (eventType == 2)
            {
                platform.KillAllProcesses();
            }
            return false;
        }

        static void CancelHandler(object? sender, ConsoleCancelEventArgs args)
        {
            platform.KillAllProcesses();
            cts.Cancel();
            args.Cancel = true;
        }

    }

}

