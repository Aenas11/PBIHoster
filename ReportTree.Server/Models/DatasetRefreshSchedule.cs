namespace ReportTree.Server.Models;

public class DatasetRefreshSchedule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public string DatasetId { get; set; } = string.Empty;
    public Guid? ReportId { get; set; }
    public int? PageId { get; set; }
    public bool Enabled { get; set; } = true;
    public string Cron { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
    public int RetryCount { get; set; } = 2;
    public int RetryBackoffSeconds { get; set; } = 120;
    public bool NotifyOnSuccess { get; set; }
    public bool NotifyOnFailure { get; set; } = true;
    public List<RefreshNotificationTarget> NotifyTargets { get; set; } = new();
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class RefreshNotificationTarget
{
    public string Type { get; set; } = "Email";
    public string Target { get; set; } = string.Empty;
}
