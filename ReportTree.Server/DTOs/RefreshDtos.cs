using ReportTree.Server.Models;

namespace ReportTree.Server.DTOs;

public record RefreshNotificationTargetDto(string Type, string Target);

public record DatasetRefreshScheduleDto(
    Guid Id,
    string Name,
    Guid WorkspaceId,
    string DatasetId,
    Guid? ReportId,
    int? PageId,
    bool Enabled,
    string Cron,
    string TimeZone,
    int RetryCount,
    int RetryBackoffSeconds,
    bool NotifyOnSuccess,
    bool NotifyOnFailure,
    List<RefreshNotificationTargetDto> NotifyTargets,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record CreateDatasetRefreshScheduleRequest(
    string Name,
    Guid WorkspaceId,
    string DatasetId,
    Guid? ReportId,
    int? PageId,
    bool Enabled,
    string Cron,
    string TimeZone,
    int? RetryCount,
    int? RetryBackoffSeconds,
    bool NotifyOnSuccess,
    bool NotifyOnFailure,
    List<RefreshNotificationTargetDto>? NotifyTargets
);

public record UpdateDatasetRefreshScheduleRequest(
    string Name,
    Guid WorkspaceId,
    string DatasetId,
    Guid? ReportId,
    int? PageId,
    bool Enabled,
    string Cron,
    string TimeZone,
    int? RetryCount,
    int? RetryBackoffSeconds,
    bool NotifyOnSuccess,
    bool NotifyOnFailure,
    List<RefreshNotificationTargetDto>? NotifyTargets
);

public record ManualDatasetRefreshRequest(
    Guid WorkspaceId,
    Guid? ReportId,
    int? PageId
);

public record DatasetRefreshRunDto(
    Guid Id,
    Guid? ScheduleId,
    Guid WorkspaceId,
    string DatasetId,
    Guid? ReportId,
    int? PageId,
    string? TriggeredByUserId,
    DateTime RequestedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    RefreshStatus Status,
    string? FailureReason,
    string? PowerBiRequestId,
    string? PowerBiActivityId,
    int RetriesAttempted,
    int? DurationMs
);
