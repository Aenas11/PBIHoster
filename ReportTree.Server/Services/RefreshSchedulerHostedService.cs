using Microsoft.Extensions.Options;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Services;

public class RefreshSchedulerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshSchedulerHostedService> _logger;
    private readonly RefreshOptions _options;

    public RefreshSchedulerHostedService(
        IServiceProvider serviceProvider,
        IOptions<RefreshOptions> options,
        ILogger<RefreshSchedulerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSchedulesAsync(stoppingToken);
                await ProcessRetriesAsync(stoppingToken);
                await SyncActiveRunsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh scheduler cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessSchedulesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleRepo = scope.ServiceProvider.GetRequiredService<IDatasetRefreshScheduleRepository>();
        var runRepo = scope.ServiceProvider.GetRequiredService<IDatasetRefreshRunRepository>();
        var refreshService = scope.ServiceProvider.GetRequiredService<DatasetRefreshService>();

        var schedules = await scheduleRepo.GetAllAsync();
        var now = DateTime.UtcNow;

        foreach (var schedule in schedules.Where(s => s.Enabled))
        {
            if (!CronScheduleHelper.TryGetNextOccurrence(schedule.Cron, schedule.TimeZone, schedule.CreatedAtUtc, out _, out var error))
            {
                _logger.LogWarning("Invalid schedule {ScheduleId}: {Error}", schedule.Id, error);
                continue;
            }

            var latestRun = await runRepo.GetLatestByScheduleIdAsync(schedule.Id);
            var fromUtc = latestRun?.RequestedAtUtc ?? schedule.CreatedAtUtc;
            if (!CronScheduleHelper.TryGetNextOccurrence(schedule.Cron, schedule.TimeZone, fromUtc, out var nextUtc, out _))
            {
                continue;
            }

            if (now < nextUtc)
            {
                continue;
            }

            try
            {
                await refreshService.TriggerScheduledAsync(schedule, "System", cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to trigger scheduled refresh for dataset {DatasetId}", schedule.DatasetId);
            }
        }
    }

    private async Task ProcessRetriesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleRepo = scope.ServiceProvider.GetRequiredService<IDatasetRefreshScheduleRepository>();
        var runRepo = scope.ServiceProvider.GetRequiredService<IDatasetRefreshRunRepository>();
        var refreshService = scope.ServiceProvider.GetRequiredService<DatasetRefreshService>();

        var failedRuns = await runRepo.GetFailedRunsAsync();
        var now = DateTime.UtcNow;

        foreach (var run in failedRuns)
        {
            if (!run.ScheduleId.HasValue || run.CompletedAtUtc == null)
            {
                continue;
            }

            var schedule = await scheduleRepo.GetByIdAsync(run.ScheduleId.Value);
            if (schedule == null || !schedule.Enabled)
            {
                continue;
            }

            if (run.RetriesAttempted >= schedule.RetryCount)
            {
                continue;
            }

            var retryAt = run.CompletedAtUtc.Value.AddSeconds(schedule.RetryBackoffSeconds);
            if (now < retryAt)
            {
                continue;
            }

            try
            {
                await refreshService.TriggerScheduledAsync(
                    schedule,
                    "System",
                    run.RetriesAttempted + 1,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retry scheduled refresh for dataset {DatasetId}", run.DatasetId);
            }
        }
    }

    private async Task SyncActiveRunsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var runRepo = scope.ServiceProvider.GetRequiredService<IDatasetRefreshRunRepository>();
        var refreshService = scope.ServiceProvider.GetRequiredService<DatasetRefreshService>();

        var activeRuns = await runRepo.GetActiveAsync();
        foreach (var run in activeRuns)
        {
            await refreshService.SyncRunStatusAsync(run, cancellationToken);
        }
    }
}
