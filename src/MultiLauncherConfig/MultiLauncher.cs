namespace multi_launcher.MultiLauncherConfig;

public class MultiLauncher
{
    public List<SpaApp> SpaApps { get; set; } = new List<SpaApp>();
    public List<Process> Processes { get; set; } = new List<Process>();
}
