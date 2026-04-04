namespace ReportTree.Server.Models;

public class UsageEvent
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}