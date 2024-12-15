namespace multi_launcher.MultiLauncherConfig;
public class Process
{
    public string Name { get; set; }
    public OSCommand Windows { get; set; }
    public OSCommand Linux { get; set; }
    public Dictionary<string, string> ProcessEnvironment { get; set; }

}