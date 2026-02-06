namespace ReportTree.Server.Models;

public enum RefreshStatus
{
    Queued,
    InProgress,
    Succeeded,
    Failed,
    Cancelled
}

public class DatasetRefreshRun
{
    public Guid Id { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string DatasetId { get; set; } = string.Empty;
    public Guid? ReportId { get; set; }
    public int? PageId { get; set; }
    public string? TriggeredByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public RefreshStatus Status { get; set; } = RefreshStatus.Queued;
    public string? FailureReason { get; set; }
    public string? PowerBiRequestId { get; set; }
    public string? PowerBiActivityId { get; set; }
    public int RetriesAttempted { get; set; }
    public int? DurationMs { get; set; }
    public DateTime? LastStatusCheckedAtUtc { get; set; }
}
