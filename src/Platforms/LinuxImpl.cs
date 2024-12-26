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

    [DllImport("libc")]
    private static extern int setpgid(int pid, int pgid);

    [DllImport("libc")]
    private static extern int getpgid(int pid);

    [DllImport("libc")]
    private static extern int killpg(int pgrp, int sig);
    
    [DllImport("libc")]
    private static extern int prctl(int option, int arg2, int arg3, int arg4, int arg5);

    private const int PR_SET_PDEATHSIG = 1;
    private const int SIGKILL = 9;
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
        try
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

            // Send a signal to the process group
            int processGroupId = Environment.ProcessId;

            // Wait for the child processes to exit gracefully within a timeout
            if (!WaitForProcessesToExit(processGroupId, timeoutMilliseconds: 4000))
            {
                Console.WriteLine("Processes did not exit gracefully. Sending SIGTERM...");
                // Send SIGTERM (or SIGKILL) for forced termination
                killpg(processGroupId, SIGTERM);
                Console.WriteLine($"Sent SIGTERM to process group {processGroupId}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to kill process group: {ex.Message}");
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
        var process = ProcessLauncher.ExecuteLaunchProcess(name, cmd, args, path, env);
       
        try
        {
            // Use the parent's PID as the process group ID
            setpgid(process.Id, Environment.ProcessId);
            Console.WriteLine($"Process {process.Id} added to process group {Environment.ProcessId}");

            // Use prctl to ensure the child dies if the parent dies
            if (prctl(PR_SET_PDEATHSIG, SIGKILL, 0, 0, 0) != 0)
            {
                throw new Exception("Failed to set PR_SET_PDEATHSIG");
            }
            Console.WriteLine($"Set PR_SET_PDEATHSIG for process {process.Id}.");

        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to configure process {process.Id}: {ex.Message}");
        }

        processList.Add(process);
    }
    private bool WaitForProcessesToExit(int processGroupId, int timeoutMilliseconds)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds)
        {
            // Query currently running child processes in the process group
            var activeProcesses = Process.GetProcesses()
                                        .Where(p =>
                                        {
                                            try
                                            {
                                                return getpgid(p.Id) == processGroupId;
                                            }
                                            catch
                                            {
                                                return false; // Process may have already exited
                                            }
                                        })
                                        .ToList();

            // If no active processes, return true
            if (!activeProcesses.Any())
            {
                return true;
            }

            // Introduce a short delay before polling again (lightweight alternative to Thread.Sleep)
            Thread.Sleep(500);
        }

        // Timeout occurred
        return false;
    }
}