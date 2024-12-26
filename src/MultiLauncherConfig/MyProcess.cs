namespace multi_launcher.MultiLauncherConfig;
public record MyProcess
{
    public string Name { get; set; } = string.Empty;
    public OSCommand Windows { get; set; } = new();
    public OSCommand Linux { get; set; } = new();
    public Dictionary<string, string> ProcessEnvironment { get; set; } = [];
}