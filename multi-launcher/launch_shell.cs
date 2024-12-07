using System;
using System.Diagnostics;
namespace multi_launcher;
static class launch_shell
{
    /// <summary>
    /// Launches arbitrary shell command
    /// </summary>
    /// <param name="name">Name used in logging</param>
    /// <param name="processFileName"></param>
    /// <param name="processArguments"></param>
    /// <param name="processWorkingDirectory"></param>
    public static void execute_launch_shell(string name, 
        string processFileName, 
        string processArguments, 
        string processWorkingDirectory,
        CancellationToken cancellationToken)
    {

        ProcessStartInfo pythonProcessStartInfo = new ()
        {
            FileName = processFileName,  // Specify the command to run
            Arguments = processArguments, // Pass in the arguments
            WorkingDirectory = processWorkingDirectory, //Path.Combine(Directory.GetCurrentDirectory(), ".."), // Set the working directory
            RedirectStandardOutput = true,               // Optionally read output
            RedirectStandardError = true,                // Optionally read errors
            UseShellExecute = false,                     // Required for redirection
            CreateNoWindow = true                        // Don't create a visible console
        };

        try
        {
            Console.WriteLine($"Starting {name} process...");

            var process = Process.Start(pythonProcessStartInfo) 
                ?? throw new Exception("Unable to retrieve process handle");

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    Console.WriteLine($"[{name}] {e.Data}");
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    Console.Error.WriteLine($"[{name} - Error] {e.Data}");
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

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
                        process.Kill(); // Terminate the process
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