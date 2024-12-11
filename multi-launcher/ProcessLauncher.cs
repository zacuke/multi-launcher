using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace multi_launcher;
static class ProcessLauncher
{

    /// <summary>
    /// Launches arbitrary shell command
    /// </summary>
    /// <param name="name">Name used in logging</param>
    /// <param name="processFileName"></param>
    /// <param name="processArguments"></param>
    /// <param name="processWorkingDirectory"></param>
    public static Process ExecuteLaunchProcess(string name,
        string processFileName,
        string processArguments,
        string processWorkingDirectory,
        CancellationToken cancellationToken)
    {

        ProcessStartInfo pythonProcessStartInfo = new()
        {
            FileName = processFileName,
            Arguments = processArguments,
            WorkingDirectory = processWorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(pythonProcessStartInfo)
            ?? throw new Exception("Unable to get handle to process");

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
        return process;
    }


}