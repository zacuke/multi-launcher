using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace multi_launcher;
static class ProcessLauncher
{
    //[DllImport("kernel32.dll", SetLastError = true)]
    //private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

    //[DllImport("libc")]
    //private static extern int kill(int pid, int sig);


    //private const int SIGINT = 2;

    /// <summary>
    /// Launches arbitrary shell command
    /// </summary>
    /// <param name="name">Name used in logging</param>
    /// <param name="processFileName"></param>
    /// <param name="processArguments"></param>
    /// <param name="processWorkingDirectory"></param>
    public static void ExecuteLaunchProcess(string name, 
        string processFileName, 
        string processArguments, 
        string processWorkingDirectory,
        CancellationToken cancellationToken)
    {

        //ProcessStartInfo pythonProcessStartInfo = new ()
        //{
        //    FileName = processFileName,  // Specify the command to run
        //    Arguments = processArguments, // Pass in the arguments
        //    WorkingDirectory = processWorkingDirectory, //Path.Combine(Directory.GetCurrentDirectory(), ".."), // Set the working directory
        //    RedirectStandardOutput = true,               // Optionally read output
        //    RedirectStandardError = true,                // Optionally read errors
        //    UseShellExecute = false,                     // Required for redirection
        //    CreateNoWindow = true                        // Don't create a visible console
        //};

        ConsoleAppManager appManager = new ConsoleAppManager(processFileName, processWorkingDirectory);
        //string[] args = new string[] { "args here" };
        appManager.ExecuteAsync(processArguments);
        //await Task.Delay(Convert.ToInt32(duration.TotalSeconds * 1000) + 20000);

        //if (appManager.Running)
        //{
        //    // If stilll running, send CTRL-C
        //    appManager.Write("\x3");
        //}


        try
        {
            Console.WriteLine($"Starting {name} process...");

            //var process = Process.Start(pythonProcessStartInfo) 
            //    ?? throw new Exception("Unable to retrieve process handle");

            //process.OutputDataReceived += (sender, e) =>
            //{
            //    if (e.Data != null)
            //        Console.WriteLine($"[{name}] {e.Data}");
            //};
            //process.ErrorDataReceived += (sender, e) =>
            //{
            //    if (e.Data != null)
            //        Console.Error.WriteLine($"[{name} - Error] {e.Data}");
            //};

            //process.BeginOutputReadLine();
            //process.BeginErrorReadLine();

            // Start a non-blocking task to monitor cancellation and process lifetime
            Task.Run(async () =>
            {
                try
                {
                    // Wait for the process to exit or cancellation to occur
                    await MonitorProcessAsync(process, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Cancellation requested. Terminating {name}...");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unexpected error in {name}: {ex.Message}");
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Cancellation requested. Terminating {name} process...");
            // Handle process termination if needed
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to start the {name} process: {ex.Message}");
            Environment.Exit(-1);
        }
    }
    /// <summary>
    /// Monitors the lifetime of a process, respecting cancellation.
    /// </summary>
    /// <param name="process">The process to monitor.</param>
    /// <param name="cancellationToken">Token that signals cancellation.</param>
    private static async Task MonitorProcessAsync(Process process, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();

        // Attach an event handler to complete the task when the process exits
        process.Exited += (sender, args) =>
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetResult();
            }
        };
        process.EnableRaisingEvents = true;

        using (cancellationToken.Register(() =>
        {
            // Handle process termination upon cancellation
            if (!tcs.Task.IsCompleted)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        //process.Kill(); // Terminate the process

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            // Send Ctrl+C on Windows
                            //GenerateConsoleCtrlEvent(0, (uint)process.Id);
                            process.write
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            // Send SIGINT on Linux/macOS
                            kill(process.Id, SIGINT);
                        }

                        Console.WriteLine($"Process {process.ProcessName} terminated due to cancellation.");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error terminating process: {ex.Message}");
                }
                tcs.TrySetCanceled(cancellationToken); // Signal task cancellation
            }
        }))
        {
            // Wait for the process to exit or for cancellation to be triggered
            await tcs.Task;
        }
    }

}