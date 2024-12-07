namespace multi_launcher
{
    class Program
    {


        static async Task Main(string[] args)
        {
            // Manually set up a cancellation token that stays under your control
            using var customCancellationTokenSource = new CancellationTokenSource();

            // Handle the Ctrl+C behavior directly
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Ctrl+C pressed, but handling is disabled.");
                e.Cancel = true; // Prevents application termination.
            };

            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    // Add your DI services here if needed

                    var spaPath = Path.GetFullPath(Path.Combine("..", "..", "soad", "trading-dashboard", "build"));
                    string bindUrl = "http://0.0.0.0:3000";
                    services.AddSingleton<IHostedService>(sp => new SpaLauncher(spaPath, bindUrl));

                }).UseConsoleLifetime(options => options.SuppressStatusMessages = true); ;

            // Create and start the host
            var host = hostBuilder.Build();

            // Run the host in the background
            var hostTask = host.RunAsync(customCancellationTokenSource.Token);

            // Insert your old Main logic here
            //await run_application(args);

            // Wait for the host to complete
            await hostTask;
        }

        static async Task run_application(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var cts = new CancellationTokenSource();

            // Gracefully handle Ctrl+C or signals to stop the app
            //Console.CancelKeyPress += (sender, e) =>
            //{
            //    Console.WriteLine("Graceful shutdown triggered...");
            //    e.Cancel = true;
            //    //e.Cancel = true; // Prevent the process from terminating immediately
            //    //cts.Cancel(); // Signal cancellation to the app
            //};

            var soadFolder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..","..", "soad"));

            launch_shell.execute_launch_shell("soad API",
                "cmd",
                "/c python main.py --mode api",
                Path.GetFullPath(soadFolder),
                cts.Token);

            //var spaPath = Path.GetFullPath(Path.Combine("..", "..", "soad", "trading-dashboard", "build"));
           // launch_spa.execute_launch_spa(spaPath, cts.Token);

            try
            {
                // Block until the cancellation token is triggered
                await WaitForShutdownAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when the cancellation token is triggered
                Console.WriteLine("Shutdown signal received. Cleaning up...");
            }

            Console.WriteLine("Application exiting.");

        }

        static async Task WaitForShutdownAsync(CancellationToken token)
        {
            // Asynchronously wait until the token is cancelled (Ctrl+C pressed)
            var tcs = new TaskCompletionSource<bool>();

            // Register a callback to observe the cancellation
            using (token.Register(() => tcs.TrySetResult(true)))
            {
                await tcs.Task; // Wait for the task to complete when the token is cancelled
            }
        }

    }
}

