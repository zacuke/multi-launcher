namespace multi_launcher;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

public class WindowsImpl : IPlatform
{
    private readonly ConsoleEventDelegate _closeHandler;
    public delegate bool ConsoleEventDelegate(int eventType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    public WindowsImpl(ConsoleEventDelegate closeHandler)
    {
        _closeHandler = closeHandler;
    }

    public void SetConsoleCtrlHandler()
    {
        SetConsoleCtrlHandler(_closeHandler, true);
    }

    public void HandleCtrlC(Action killAllProcesses, CancellationTokenSource cts)
    {
        Console.CancelKeyPress += (sender, args) =>
        {
            killAllProcesses();
            cts.Cancel();
            args.Cancel = true;
        };
    }

    public void GenerateCtrlCEvent(uint processId)
    {
        FreeConsole();
        AttachConsole(processId);
        GenerateConsoleCtrlEvent(0, 0);
    }

    public IList<Process> GetChildProcesses(Process process)
    {
        var childProcesses = new List<Process>();

        // Use ManagementObjectSearcher to find immediate child processes
        var result = new ManagementObjectSearcher(
            $"Select * From Win32_Process Where ParentProcessID={process.Id}")
            .Get()
            .Cast<ManagementObject>();

        // For each immediate child process, retrieve the process and add to the list
        foreach (var mo in result)
        {
            var childProcess = Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]));
            childProcesses.Add(childProcess);

            // Recursively get children of this child process
            childProcesses.AddRange(GetChildProcesses(childProcess));
        }

        return childProcesses;
    }
}