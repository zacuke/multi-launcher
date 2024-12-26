namespace multi_launcher.MultiLauncherConfig;

public record OSCommand 
{
    public string Path { get; set; } = string.Empty;
    public string Cmd { get; set; } = string.Empty;
    public string Args { get; set; } = string.Empty;
}