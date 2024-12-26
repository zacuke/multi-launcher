namespace multi_launcher.Platforms;

using System.Diagnostics;
using WmiLight;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using multi_launcher.Launchers;

using static WindowsNativeMethods;
using static WindowsJobObjectInfo;
using multi_launcher.MultiLauncherConfig;

[SupportedOSPlatform("windows")]
public class WindowsImpl : IPlatform
{
    readonly static List<Process> processList = [];


    private readonly ConsoleEventDelegate _closeHandler;
    public delegate bool ConsoleEventDelegate(int eventType);

    private readonly IntPtr _jobHandle; // Job Object Handle

    public WindowsImpl(ConsoleEventDelegate closeHandler)
    {
        _closeHandler = closeHandler;

        // Create the Job Object and set the KILL_ON_JOB_CLOSE flag
        _jobHandle = CreateJobObject(IntPtr.Zero, null);
        if (_jobHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create Job object. Error Code: {Marshal.GetLastWin32Error()}");
        }

        // Configure Job Object to kill all processes on closure
        var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        if (!SetInformationJobObject(
                _jobHandle,
                JobObjectExtendedLimitInformation,
                ref extendedInfo,
                (uint)Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()))
        {
            throw new InvalidOperationException($"Failed to set Job object information. Error Code: {Marshal.GetLastWin32Error()}");
        }

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

        // Poll the job object to check if all processes have exited
        while (true)
        {
            var basicAccountingInfo = new JOBOBJECT_BASIC_ACCOUNTING_INFORMATION();
            if (!QueryInformationJobObject(
                    _jobHandle,
                    JobObjectBasicAccountingInformation,
                    ref basicAccountingInfo,
                    (uint)Marshal.SizeOf<JOBOBJECT_BASIC_ACCOUNTING_INFORMATION>(),
                    IntPtr.Zero))
            {
                throw new InvalidOperationException($"Failed to query Job object information. Error Code: {Marshal.GetLastWin32Error()}");
            }

            if (basicAccountingInfo.ActiveProcesses == 0)
            {
                break;
            }

            Thread.Sleep(500); // Polling interval
        }

    }

    public bool IsWindows()
    {
        return true;
    }
    public void LaunchProcess(string name, string cmd, string args, string path, Dictionary<string, string> env)
    {
        var process = ProcessLauncher.ExecuteLaunchProcess(name, cmd, args, path, env);
        processList.Add(process);

        // Assign the process to the job
        if (!AssignProcessToJobObject(_jobHandle, process.Handle))
        {
            throw new InvalidOperationException($"Failed to assign process {process.Id} to Job object. Error Code: {Marshal.GetLastWin32Error()}");
        }
    }

}