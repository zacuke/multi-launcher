using System.Diagnostics;

namespace multi_launcher;
public interface IPlatform
{
    void SetConsoleCtrlHandler();
    void HandleCtrlC(Action killAllProcesses, CancellationTokenSource cts);
    void GenerateCtrlCEvent(uint processId);

    IList<Process> GetChildProcesses(Process process);
}