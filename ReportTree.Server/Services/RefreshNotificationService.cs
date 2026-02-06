using System.Net.Http.Json;
using ReportTree.Server.Models;

namespace ReportTree.Server.Services;

public class RefreshNotificationService
{
    private readonly ILogger<RefreshNotificationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EmailService _emailService;

    public RefreshNotificationService(
        ILogger<RefreshNotificationService> logger,
        IHttpClientFactory httpClientFactory,
        EmailService emailService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _emailService = emailService;
    }

    public Task NotifyAsync(DatasetRefreshRun run, DatasetRefreshSchedule schedule)
    {
        if (schedule.NotifyTargets.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (!ShouldNotify(run.Status, schedule))
        {
            return Task.CompletedTask;
        }

        var tasks = schedule.NotifyTargets.Select(target => SendNotificationAsync(run, schedule, target));
        return Task.WhenAll(tasks);
    }

    private async Task SendNotificationAsync(
        DatasetRefreshRun run,
        DatasetRefreshSchedule schedule,
        RefreshNotificationTarget target)
    {
        if (string.Equals(target.Type, "Webhook", StringComparison.OrdinalIgnoreCase))
        {
            await SendWebhookAsync(run, schedule, target.Target);
            return;
        }

        await SendEmailAsync(run, schedule, target.Target);
    }

    private static bool ShouldNotify(RefreshStatus status, DatasetRefreshSchedule schedule)
    {
        return status switch
        {
            RefreshStatus.Succeeded => schedule.NotifyOnSuccess,
            RefreshStatus.Failed => schedule.NotifyOnFailure,
            RefreshStatus.Cancelled => schedule.NotifyOnFailure,
            _ => false
        };
    }

    private async Task SendEmailAsync(DatasetRefreshRun run, DatasetRefreshSchedule schedule, string toAddress)
    {
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            return;
        }

        var subject = $"Dataset refresh {run.Status}: {schedule.Name}";
        var body = string.Join('\n', new[]
        {
            $"Schedule: {schedule.Name}",
            $"Dataset: {run.DatasetId}",
            $"Workspace: {run.WorkspaceId}",
            $"Status: {run.Status}",
            $"Requested: {run.RequestedAtUtc:O}",
            $"Started: {(run.StartedAtUtc.HasValue ? run.StartedAtUtc.Value.ToString("O") : "-")}",
            $"Completed: {(run.CompletedAtUtc.HasValue ? run.CompletedAtUtc.Value.ToString("O") : "-")}",
            $"Duration (ms): {(run.DurationMs.HasValue ? run.DurationMs.Value.ToString() : "-")}",
            $"Failure reason: {run.FailureReason ?? "-"}",
            $"Power BI Request ID: {run.PowerBiRequestId ?? "-"}",
            $"Power BI Activity ID: {run.PowerBiActivityId ?? "-"}"
        });

        try
        {
            await _emailService.SendAsync(toAddress, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email notification failed for dataset {DatasetId}", run.DatasetId);
        }
    }

    private async Task SendWebhookAsync(DatasetRefreshRun run, DatasetRefreshSchedule schedule, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        var payload = new
        {
            scheduleId = schedule.Id,
            scheduleName = schedule.Name,
            datasetId = run.DatasetId,
            workspaceId = run.WorkspaceId,
            status = run.Status.ToString(),
            requestedAtUtc = run.RequestedAtUtc,
            startedAtUtc = run.StartedAtUtc,
            completedAtUtc = run.CompletedAtUtc,
            durationMs = run.DurationMs,
            failureReason = run.FailureReason,
            powerBiRequestId = run.PowerBiRequestId,
            powerBiActivityId = run.PowerBiActivityId
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var response = await client.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webhook notification failed for dataset {DatasetId}: {Status}",
                    run.DatasetId,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook notification failed for dataset {DatasetId}", run.DatasetId);
        }
    }
}
