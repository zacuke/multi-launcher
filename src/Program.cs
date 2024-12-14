﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace multi_launcher
{
    class Program
    {
 
        readonly static List<Process> processList = [];
        readonly static CancellationTokenSource cts = new();
        readonly static IPlatform platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
           ? new WindowsImpl(CloseHandler)
           : new LinuxImpl();


        static async Task Main(string[] args)
        {

            if (args.Length == 0)
            {
                //setup handler for when app closes
                platform.SetConsoleCtrlHandler();

                //setup handler for when ctrl-c is pressed
                platform.HandleCtrlC(KillAllProcesses, cts);


                var hostBuilder = Host.CreateDefaultBuilder(args)
                    .ConfigureServices(services =>
                    {
                        var spaPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? "c:\\src\\soad\\trading-dashboard"
                            : "/home/lyle/src/soad/trading-dashboard";

                        string bindUrl = "http://0.0.0.0:3000";
                        services.AddSingleton<IHostedService>(sp => new SpaLauncher(spaPath, bindUrl));
                        services.AddSingleton<IHostLifetime, DisableCtrlCLifeTime>();


                    });

                var soadFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "c:\\src\\soad"
                    : "/home/lyle/src/soad";

                processList.Add(ProcessLauncher.ExecuteLaunchProcess("soad API",
                    //"C:\\Users\\garth\\AppData\\Local\\Programs\\Python\\Python313\\python.exe",
                    //"main.py --mode api",
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "bash",
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c python main.py --mode api" : "python main.py --mode api",
                    Path.GetFullPath(soadFolder),
                    cts.Token));

                var host = hostBuilder.Build();
                var hostTask = host.RunAsync(cts.Token);
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
            platform.KillAllProcesses(processList);
        }

    }
}

