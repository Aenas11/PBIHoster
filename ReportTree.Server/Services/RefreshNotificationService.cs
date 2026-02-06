using ReportTree.Server.Models;

namespace ReportTree.Server.Services;

public class RefreshNotificationService
{
    private readonly ILogger<RefreshNotificationService> _logger;

    public RefreshNotificationService(ILogger<RefreshNotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(DatasetRefreshRun run, DatasetRefreshSchedule schedule)
    {
        if (schedule.NotifyTargets.Count == 0)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Refresh notification queued for dataset {DatasetId} (status: {Status}, targets: {TargetCount})",
            run.DatasetId,
            run.Status,
            schedule.NotifyTargets.Count);

        return Task.CompletedTask;
    }
}
