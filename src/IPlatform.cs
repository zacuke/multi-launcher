using System.Diagnostics;

namespace multi_launcher;
public interface IPlatform
{
    void SetConsoleCtrlHandler();
    void HandleCtrlC(Action killAllProcesses, CancellationTokenSource cts);
    void GenerateCtrlCEvent(uint processId);
    void KillAllProcesses(List<Process> processList);
    IList<Process> GetChildProcesses(Process process);
}