namespace multi_launcher.MultiLauncherConfig;
public class SpaApp
{
    public string Name { get; set; }
    public string IndexHtml { get; set; }
    public List<string> BindUrls { get; set; }
    public string SpaResponseContentType { get; set; }
    public Dictionary<string, string> SpaResponseHeaders { get; set; }
    public string WindowsPath { get; set; }
    public string LinuxPath { get; set; }
}