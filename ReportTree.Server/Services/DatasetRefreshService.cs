using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Services;

public class DatasetRefreshService
{
    private readonly IDatasetRefreshScheduleRepository _scheduleRepository;
    private readonly IDatasetRefreshRunRepository _runRepository;
    private readonly IPowerBIService _powerBiService;
    private readonly RefreshNotificationService _notificationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DatasetRefreshService> _logger;
    private readonly RefreshOptions _options;
    private readonly string _powerBiApiUrl;

    public DatasetRefreshService(
        IDatasetRefreshScheduleRepository scheduleRepository,
        IDatasetRefreshRunRepository runRepository,
        IPowerBIService powerBiService,
        RefreshNotificationService notificationService,
        IHttpClientFactory httpClientFactory,
        IOptions<RefreshOptions> options,
        IConfiguration configuration,
        ILogger<DatasetRefreshService> logger)
    {
        _scheduleRepository = scheduleRepository;
        _runRepository = runRepository;
        _powerBiService = powerBiService;
        _notificationService = notificationService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
        _powerBiApiUrl = configuration["PowerBI:ApiUrl"] ?? "https://api.powerbi.com";
    }

    public async Task<DatasetRefreshRun> TriggerManualAsync(
        string datasetId,
        Guid workspaceId,
        Guid? reportId,
        int? pageId,
        string triggeredBy,
        CancellationToken cancellationToken = default)
    {
        await EnforceManualCooldownAsync(datasetId, cancellationToken);
        await EnforceConcurrencyAsync(datasetId, cancellationToken);

        var run = new DatasetRefreshRun
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            DatasetId = datasetId,
            ReportId = reportId,
            PageId = pageId,
            TriggeredByUserId = triggeredBy,
            RequestedAtUtc = DateTime.UtcNow,
            Status = RefreshStatus.Queued
        };

        await _runRepository.AddAsync(run);
        await TriggerPowerBiRefreshAsync(run, cancellationToken);
        return run;
    }

    public async Task<DatasetRefreshRun> TriggerScheduledAsync(
        DatasetRefreshSchedule schedule,
        string triggeredBy,
        int retriesAttempted = 0,
        CancellationToken cancellationToken = default)
    {
        ValidateSchedule(schedule);
        await EnforceConcurrencyAsync(schedule.DatasetId, cancellationToken);

        var run = new DatasetRefreshRun
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            WorkspaceId = schedule.WorkspaceId,
            DatasetId = schedule.DatasetId,
            ReportId = schedule.ReportId,
            PageId = schedule.PageId,
            TriggeredByUserId = triggeredBy,
            RequestedAtUtc = DateTime.UtcNow,
            Status = RefreshStatus.Queued,
            RetriesAttempted = retriesAttempted
        };

        await _runRepository.AddAsync(run);
        await TriggerPowerBiRefreshAsync(run, cancellationToken);
        return run;
    }

    public async Task SyncRunStatusAsync(DatasetRefreshRun run, CancellationToken cancellationToken = default)
    {
        if (run.Status != RefreshStatus.Queued && run.Status != RefreshStatus.InProgress)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (run.LastStatusCheckedAtUtc.HasValue &&
            now - run.LastStatusCheckedAtUtc.Value < TimeSpan.FromSeconds(_options.PollIntervalSeconds))
        {
            return;
        }

        run.LastStatusCheckedAtUtc = now;

        var latest = await GetLatestRefreshAsync(run.WorkspaceId, run.DatasetId, cancellationToken);
        if (latest == null)
        {
            await _runRepository.UpdateAsync(run);
            return;
        }

        var status = MapStatus(latest.Status);
        if (status == run.Status)
        {
            await _runRepository.UpdateAsync(run);
            return;
        }

        run.Status = status;
        run.StartedAtUtc ??= latest.StartedAtUtc;
        run.CompletedAtUtc = latest.CompletedAtUtc ?? run.CompletedAtUtc;
        run.PowerBiRequestId ??= latest.RequestId;
        run.PowerBiActivityId ??= latest.ActivityId;
        run.FailureReason = latest.FailureReason;
        if (run.StartedAtUtc.HasValue && run.CompletedAtUtc.HasValue)
        {
            run.DurationMs = (int)(run.CompletedAtUtc.Value - run.StartedAtUtc.Value).TotalMilliseconds;
        }

        await _runRepository.UpdateAsync(run);

        if (run.Status is RefreshStatus.Succeeded or RefreshStatus.Failed or RefreshStatus.Cancelled)
        {
            var schedule = run.ScheduleId.HasValue
                ? await _scheduleRepository.GetByIdAsync(run.ScheduleId.Value)
                : null;

            if (schedule != null)
            {
                await _notificationService.NotifyAsync(run, schedule);
            }
        }
    }

    public void ValidateSchedule(DatasetRefreshSchedule schedule)
    {
        if (!CronScheduleHelper.TryValidate(schedule.Cron, schedule.TimeZone, out var error))
        {
            throw new InvalidOperationException($"Invalid schedule: {error}");
        }
    }

    private async Task EnforceManualCooldownAsync(string datasetId, CancellationToken cancellationToken)
    {
        var lastRun = await _runRepository.GetLatestByDatasetIdAsync(datasetId);
        if (lastRun == null)
        {
            return;
        }

        var cooldown = TimeSpan.FromSeconds(_options.ManualCooldownSeconds);
        if (DateTime.UtcNow - lastRun.RequestedAtUtc < cooldown)
        {
            throw new InvalidOperationException("Manual refresh cooldown window has not elapsed.");
        }
    }

    private async Task EnforceConcurrencyAsync(string datasetId, CancellationToken cancellationToken)
    {
        var active = await _runRepository.GetActiveAsync();
        var activeForDataset = active.Count(r => r.DatasetId == datasetId);
        if (activeForDataset >= _options.MaxConcurrentPerDataset)
        {
            throw new InvalidOperationException("A refresh is already running for this dataset.");
        }
    }

    private async Task TriggerPowerBiRefreshAsync(DatasetRefreshRun run, CancellationToken cancellationToken)
    {
        try
        {
            var result = await PostDatasetRefreshAsync(run.WorkspaceId, run.DatasetId, cancellationToken);
            run.StartedAtUtc = DateTime.UtcNow;
            run.Status = RefreshStatus.InProgress;
            run.PowerBiRequestId = result.RequestId;
            run.PowerBiActivityId = result.ActivityId;
        }
        catch (Exception ex)
        {
            run.Status = RefreshStatus.Failed;
            run.CompletedAtUtc = DateTime.UtcNow;
            run.FailureReason = ex.Message;
            _logger.LogWarning(ex, "Failed to start refresh for dataset {DatasetId}", run.DatasetId);
        }
        finally
        {
            await _runRepository.UpdateAsync(run);
        }
    }

    private async Task<PowerBiRefreshResult> PostDatasetRefreshAsync(
        Guid workspaceId,
        string datasetId,
        CancellationToken cancellationToken)
    {
        var token = await _powerBiService.GetAccessTokenAsync(cancellationToken);
        var client = _httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_powerBiApiUrl}/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/refreshes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return new PowerBiRefreshResult(
                RequestId: response.Headers.TryGetValues("RequestId", out var requestIds)
                    ? requestIds.FirstOrDefault()
                    : null,
                ActivityId: response.Headers.TryGetValues("ActivityId", out var activityIds)
                    ? activityIds.FirstOrDefault()
                    : null
            );
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode == (HttpStatusCode)429)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds.ToString() ?? "unknown";
            throw new InvalidOperationException($"Power BI throttled the request. Retry after {retryAfter} seconds.");
        }

        throw new InvalidOperationException($"Power BI refresh failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
    }

    private async Task<PowerBiRefreshHistoryItem?> GetLatestRefreshAsync(
        Guid workspaceId,
        string datasetId,
        CancellationToken cancellationToken)
    {
        var token = await _powerBiService.GetAccessTokenAsync(cancellationToken);
        var client = _httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_powerBiApiUrl}/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/refreshes?$top=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to fetch refresh history for dataset {DatasetId}: {Status} {Body}",
                datasetId,
                response.StatusCode,
                body);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var history = JsonSerializer.Deserialize<PowerBiRefreshHistoryResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return history?.Value?.FirstOrDefault();
    }

    private static RefreshStatus MapStatus(string? status)
    {
        return status?.ToLowerInvariant() switch
        {
            "completed" => RefreshStatus.Succeeded,
            "failed" => RefreshStatus.Failed,
            "cancelled" => RefreshStatus.Cancelled,
            "inprogress" => RefreshStatus.InProgress,
            "unknown" => RefreshStatus.Queued,
            _ => RefreshStatus.Queued
        };
    }

    private sealed record PowerBiRefreshResult(string? RequestId, string? ActivityId);

    private sealed class PowerBiRefreshHistoryResponse
    {
        public List<PowerBiRefreshHistoryItem> Value { get; set; } = new();
    }

    private sealed class PowerBiRefreshHistoryItem
    {
        public string? Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? RequestId { get; set; }
        public string? ActivityId { get; set; }
        public string? ServiceExceptionJson { get; set; }

        public DateTime? StartedAtUtc => StartTime;
        public DateTime? CompletedAtUtc => EndTime;
        public string? FailureReason => ServiceExceptionJson;
    }
}
