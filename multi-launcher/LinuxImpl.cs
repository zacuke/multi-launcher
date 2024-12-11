using System.Diagnostics;

namespace multi_launcher;
public class LinuxImpl : IPlatform
{
    public void SetConsoleCtrlHandler()
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
}