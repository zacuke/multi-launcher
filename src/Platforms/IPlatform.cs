using System.Diagnostics;

namespace multi_launcher.Platforms;
public interface IPlatform
{
    void MySetConsoleCtrlHandler();
    void HandleCtrlC(Action killAllProcesses, CancellationTokenSource cts);
    void GenerateCtrlCEvent(uint processId);
    void KillAllProcesses();
    IList<Process> GetChildProcesses(Process process);
    bool IsWindows();

    void LaunchProcess(string name, string cmd, string args, string path, Dictionary<string,string> env);

}