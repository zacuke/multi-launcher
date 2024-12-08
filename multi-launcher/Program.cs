namespace multi_launcher
{
    class Program
    {


        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();

            // Handle the Ctrl+C behavior directly
            Console.CancelKeyPress += async (sender, e) =>
            {
                Console.WriteLine("Ctrl+C has been pressed, stopping...");
                e.Cancel = true; // Prevents application termination.
                await cts.CancelAsync();
                //Environment.Exit(0);
            };

            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    // Add your DI services here if needed
                    var spaPath = Path.GetFullPath(Path.Combine("..", "..", "soad", "trading-dashboard", "build"));
                    string bindUrl = "http://0.0.0.0:3000";
                    services.AddSingleton<IHostedService>(sp => new SpaLauncher(spaPath, bindUrl));
                    services.AddSingleton<IHostLifetime, NoopConsoleLifetime>();


                });//.UseConsoleLifetime(options => options.SuppressStatusMessages = true); ;

            // Create and start the host
            var host = hostBuilder.Build();
            var hostTask = host.RunAsync(cts.Token);

            await run_application(args, cts.Token);

            await hostTask;
        }

        static async Task run_application(string[] args, CancellationToken cancellationToken)
        {

            var soadFolder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..","..", "soad"));

            ProcessLauncher.ExecuteLaunchProcess("soad API",
                "C:\\Users\\garth\\.pyenv\\pyenv-win\\versions\\3.11.9\\python.exe",
                "main.py --mode api",
                Path.GetFullPath(soadFolder),
                cancellationToken);

            try
            {
                await WaitForShutdownAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Shutdown signal received. Cleaning up...");
            }

            Console.WriteLine("Application exiting.");

        }

        static async Task WaitForShutdownAsync(CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (token.Register(() => tcs.TrySetResult(true)))
            {
                await tcs.Task; 
            }
        }

    }
}

