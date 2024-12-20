﻿namespace multi_launcher.Platforms;

using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
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

    public void KillAllProcesses(List<Process> processList)
    {

        var currPath = Environment.ProcessPath
            ?? System.Reflection.Assembly.GetExecutingAssembly().Location;

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
}