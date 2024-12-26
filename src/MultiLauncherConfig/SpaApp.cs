namespace multi_launcher.MultiLauncherConfig;
public record SpaApp
{
    public string Name { get; set; } = string.Empty;
    public string IndexHtml { get; set; } = string.Empty;
    public List<string> BindUrls { get; set; } = [];
    public string SpaResponseContentType { get; set; } = string.Empty;
    public Dictionary<string, string> SpaResponseHeaders { get; set; } = [];
    public string WindowsPath { get; set; } = string.Empty;
    public string LinuxPath { get; set; } = string.Empty;
}