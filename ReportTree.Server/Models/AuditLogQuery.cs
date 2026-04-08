namespace ReportTree.Server.Models;

public class AuditLogQuery
{
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 100;
    public string? Username { get; set; }
    public string? ActionType { get; set; }
    public string? Resource { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public bool? Success { get; set; }
}