using multi_launcher.Launchers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace multi_launcher.Platforms;
public class LinuxImpl : IPlatform
{
    readonly static List<Process> processList = [];


    [DllImport("libc")]
    private static extern int kill(int pid, int sig);

    // Import the `strerror` function from libc to handle errors
    [DllImport("libc")]
    private static extern nint strerror(int errnum);

    public void MySetConsoleCtrlHandler()
    {
        // Linux does not use SetConsoleCtrlHandler
        // Implement Linux-specific cleanup mechanism if needed
    }

    public void HandleCtrlC(Action killAllProcesses, CancellationTokenSource cts)
    {
        Console.CancelKeyPress += (sender, args) =>
        {
            killAllProcesses();
            cts.Cancel();
            args.Cancel = true; // Terminate gracefully
        };
    }

    public void GenerateCtrlCEvent(uint processId)
    {
        // Handle control signals for Linux
        throw new NotImplementedException("Linux GenerateCtrlCEvent not implemented.");
    }

    public IList<Process> GetChildProcesses(Process process)
    {
        var childProcesses = new List<Process>();
        int parentPid = process.Id;

        var procDir = new DirectoryInfo("/proc");

        try
        {
            // Iterate through all directories in /proc (potential PIDs)
            foreach (var dir in procDir.GetDirectories())
            {
                // Skip if not a valid PID directory
                if (!int.TryParse(dir.Name, out int pid))
                    continue;

                // Read the status file for each process
                string statusPath = Path.Combine(dir.FullName, "status");
                if (!File.Exists(statusPath))
                    continue; // Skip if the status file doesn't exist (e.g., process has exited)

                // Read PPid information from the status file
                var lines = File.ReadAllLines(statusPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("PPid:"))
                    {
                        int reportedParentPid = int.Parse(line.Split(':')[1].Trim());
                        if (reportedParentPid == parentPid)
                        {
                            try
                            {
                                // Add the process to the list
                                childProcesses.Add(Process.GetProcessById(pid));
                            }
                            catch (ArgumentException)
                            {
                                // The process might have exited between the time we discovered it and now
                                // Just skip it
                                continue;
                            }
                        }
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error retrieving child processes for PID {parentPid}: {ex.Message}");
        }

        return childProcesses;
    }

    // Constants for signals
    private const int SIGINT = 2;  // Interrupt signal (Ctrl+C equivalent)
    private const int SIGTERM = 15; // Termination signal

    public void KillAllProcesses()
    {
        // Step 1: Send SIGINT (graceful termination, equivalent to Ctrl+C)
        foreach (var process in processList)
        {
            // Send SIGINT to child processes first
            var childProcesses = GetChildProcesses(process);
            foreach (var childProcess in childProcesses)
            {
                SendSignal(childProcess.Id, SIGINT);
            }

            // Send SIGINT to the parent process
            SendSignal(process.Id, SIGINT);
        }

        // Allow time for the processes to terminate gracefully
        Thread.Sleep(4000); // Sleep for 4 seconds

        // Step 2: Send SIGTERM (force termination if SIGINT didn’t work)
        foreach (var process in processList)
        {
            // Send SIGTERM to child processes first
            var childProcesses = GetChildProcesses(process);
            foreach (var childProcess in childProcesses)
            {
                SendSignal(childProcess.Id, SIGTERM);
            }

            // Send SIGTERM to the parent process
            SendSignal(process.Id, SIGTERM);
        }
    }

    private void SendSignal(int pid, int signal)
    {
        int result = kill(pid, signal);
        if (result != 0) // If `kill` returns -1, an error occurred
        {
            var errno = Marshal.GetLastPInvokeError();
            string errorMessage = GetStrError(errno);
            Console.WriteLine($"Failed to send signal {signal} to process {pid}. Error: {errorMessage}");
        }
        else
        {
            Console.WriteLine($"Signal {signal} sent to process {pid}.");
        }
    }

    private string GetStrError(int errnum)
    {
        nint errorMessagePtr = strerror(errnum); // Get the error message string
        return Marshal.PtrToStringAnsi(errorMessagePtr) ?? $"Unknown error ({errnum})";
    }

    public bool IsWindows()
    {
        return false;
    }
    public void LaunchProcess(string name, string cmd, string args, string path, Dictionary<string, string> env)
    {
        processList.Add( ProcessLauncher.ExecuteLaunchProcess(name, cmd, args, path, env));

    }

}