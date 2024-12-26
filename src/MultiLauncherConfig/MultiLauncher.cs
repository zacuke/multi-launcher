namespace multi_launcher.MultiLauncherConfig;

public record MultiLauncher
{
    public List<SpaApp> SpaApps { get; set; } = [];
    public List<MyProcess> Processes { get; set; } = [];
}
