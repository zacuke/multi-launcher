namespace multi_launcher.Platforms;

using System.Diagnostics;
using WmiLight;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using multi_launcher.Launchers;

using static WindowsNativeMethods;

[SupportedOSPlatform("windows")]
public class WindowsImpl : IPlatform
{
    readonly static List<Process> processList = [];


    private readonly ConsoleEventDelegate _closeHandler;
    public delegate bool ConsoleEventDelegate(int eventType);


    public WindowsImpl(ConsoleEventDelegate closeHandler)
    {
        _closeHandler = closeHandler;
    }

    public void MySetConsoleCtrlHandler()
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

        ////  System.Management Doesn't Support Trimming Yet
        ////  https://github.com/dotnet/runtime/issues/61960

        // Use ManagementObjectSearcher to find immediate child processes
        //var result = new ManagementObjectSearcher(
        //    $"Select * From Win32_Process Where ParentProcessID={process.Id}")
        //    .Get()
        //    .Cast<ManagementObject>();

        //use wmilight instead
        using WmiConnection con = new () ;
        var result = con.CreateQuery($"SELECT * FROM Win32_Process Where ParentProcessID={process.Id}");


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

    public void KillAllProcesses()
    {

        var currPath = Environment.ProcessPath
            ?? AppContext.BaseDirectory;

        //send ctrl-c first
        foreach (var process in processList)
        {
            var childProcesses = GetChildProcesses(process);
            foreach (var i in childProcesses)
            {
                Process.Start(currPath, i.Id.ToString());

            }
            Process.Start(currPath, process.Id.ToString());
        }

        Thread.Sleep(4);

        //then kill any left
        foreach (var process in processList)
        {
            var childProcesses = GetChildProcesses(process);
            foreach (var i in childProcesses)
            {
                i.Kill();
            }
            process.Kill();
        }
    }

    public bool IsWindows()
    {
        return true;
    }
    public void LaunchProcess(string name, string cmd, string args, string path, Dictionary<string, string> env)
    {
        processList.Add(ProcessLauncher.ExecuteLaunchProcess(name, cmd, args, path, env));

    }

}