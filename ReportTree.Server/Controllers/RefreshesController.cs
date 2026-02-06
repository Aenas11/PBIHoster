using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ReportTree.Server.DTOs;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Services;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RefreshesController : ControllerBase
{
    private readonly IDatasetRefreshScheduleRepository _scheduleRepository;
    private readonly IDatasetRefreshRunRepository _runRepository;
    private readonly DatasetRefreshService _refreshService;
    private readonly AuditLogService _auditLogService;
    private readonly ILogger<RefreshesController> _logger;
    private readonly RefreshOptions _defaults;

    public RefreshesController(
        IDatasetRefreshScheduleRepository scheduleRepository,
        IDatasetRefreshRunRepository runRepository,
        DatasetRefreshService refreshService,
        AuditLogService auditLogService,
        IOptions<RefreshOptions> options,
        ILogger<RefreshesController> logger)
    {
        _scheduleRepository = scheduleRepository;
        _runRepository = runRepository;
        _refreshService = refreshService;
        _auditLogService = auditLogService;
        _logger = logger;
        _defaults = options.Value;
    }

    [HttpPost("datasets/{datasetId}/run")]
    public async Task<ActionResult<DatasetRefreshRunDto>> RunDatasetRefresh(
        string datasetId,
        [FromBody] ManualDatasetRefreshRequest request,
        CancellationToken cancellationToken)
    {
        var username = User.Identity?.Name ?? "Unknown";

        try
        {
            var run = await _refreshService.TriggerManualAsync(
                datasetId,
                request.WorkspaceId,
                request.ReportId,
                request.PageId,
                username,
                cancellationToken);

            await _auditLogService.LogAsync("DATASET_REFRESH_RUN", datasetId, "Manual refresh triggered");
            return Ok(ToDto(run));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogAsync("DATASET_REFRESH_RUN", datasetId, ex.Message, false);
            _logger.LogWarning(ex, "Manual refresh failed for dataset {DatasetId}", datasetId);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("datasets/{datasetId}/history")]
    public async Task<ActionResult<IEnumerable<DatasetRefreshRunDto>>> GetDatasetHistory(
        string datasetId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var runs = await _runRepository.GetByDatasetIdAsync(datasetId, skip, take);
        return Ok(runs.Select(ToDto));
    }

    [HttpGet("schedules")]
    public async Task<ActionResult<IEnumerable<DatasetRefreshScheduleDto>>> GetSchedules()
    {
        var schedules = await _scheduleRepository.GetAllAsync();
        return Ok(schedules.Select(ToDto));
    }

    [HttpPost("schedules")]
    public async Task<ActionResult<DatasetRefreshScheduleDto>> CreateSchedule([FromBody] CreateDatasetRefreshScheduleRequest request)
    {
        var username = User.Identity?.Name ?? "Unknown";
        var schedule = new DatasetRefreshSchedule
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            WorkspaceId = request.WorkspaceId,
            DatasetId = request.DatasetId,
            ReportId = request.ReportId,
            PageId = request.PageId,
            Enabled = request.Enabled,
            Cron = request.Cron,
            TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "UTC" : request.TimeZone,
            RetryCount = request.RetryCount ?? _defaults.DefaultRetryCount,
            RetryBackoffSeconds = request.RetryBackoffSeconds ?? _defaults.DefaultRetryBackoffSeconds,
            NotifyOnSuccess = request.NotifyOnSuccess,
            NotifyOnFailure = request.NotifyOnFailure,
            NotifyTargets = MapTargets(request.NotifyTargets),
            CreatedByUserId = username,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        try
        {
            _refreshService.ValidateSchedule(schedule);
            await _scheduleRepository.CreateAsync(schedule);
            await _auditLogService.LogAsync("DATASET_REFRESH_SCHEDULE_CREATE", schedule.DatasetId, "Schedule created");
            return CreatedAtAction(nameof(GetSchedules), new { id = schedule.Id }, ToDto(schedule));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogAsync("DATASET_REFRESH_SCHEDULE_CREATE", schedule.DatasetId, ex.Message, false);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("schedules/{id:guid}")]
    public async Task<ActionResult<DatasetRefreshScheduleDto>> UpdateSchedule(
        Guid id,
        [FromBody] UpdateDatasetRefreshScheduleRequest request)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(id);
        if (schedule == null)
        {
            return NotFound();
        }

        schedule.Name = request.Name;
        schedule.WorkspaceId = request.WorkspaceId;
        schedule.DatasetId = request.DatasetId;
        schedule.ReportId = request.ReportId;
        schedule.PageId = request.PageId;
        schedule.Enabled = request.Enabled;
        schedule.Cron = request.Cron;
        schedule.TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "UTC" : request.TimeZone;
        schedule.RetryCount = request.RetryCount ?? schedule.RetryCount;
        schedule.RetryBackoffSeconds = request.RetryBackoffSeconds ?? schedule.RetryBackoffSeconds;
        schedule.NotifyOnSuccess = request.NotifyOnSuccess;
        schedule.NotifyOnFailure = request.NotifyOnFailure;
        schedule.NotifyTargets = MapTargets(request.NotifyTargets);
        schedule.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            _refreshService.ValidateSchedule(schedule);
            await _scheduleRepository.UpdateAsync(schedule);
            await _auditLogService.LogAsync("DATASET_REFRESH_SCHEDULE_UPDATE", schedule.DatasetId, "Schedule updated");
            return Ok(ToDto(schedule));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogAsync("DATASET_REFRESH_SCHEDULE_UPDATE", schedule.DatasetId, ex.Message, false);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("schedules/{id:guid}")]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(id);
        if (schedule == null)
        {
            return NotFound();
        }

        await _scheduleRepository.DeleteAsync(id);
        await _auditLogService.LogAsync("DATASET_REFRESH_SCHEDULE_DELETE", schedule.DatasetId, "Schedule deleted");
        return NoContent();
    }

    [HttpPost("schedules/{id:guid}/toggle")]
    public async Task<ActionResult<DatasetRefreshScheduleDto>> ToggleSchedule(Guid id)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(id);
        if (schedule == null)
        {
            return NotFound();
        }

        schedule.Enabled = !schedule.Enabled;
        schedule.UpdatedAtUtc = DateTime.UtcNow;
        await _scheduleRepository.UpdateAsync(schedule);
        await _auditLogService.LogAsync("DATASET_REFRESH_SCHEDULE_TOGGLE", schedule.DatasetId, schedule.Enabled ? "Enabled" : "Disabled");
        return Ok(ToDto(schedule));
    }

    private static DatasetRefreshScheduleDto ToDto(DatasetRefreshSchedule schedule)
    {
        return new DatasetRefreshScheduleDto(
            schedule.Id,
            schedule.Name,
            schedule.WorkspaceId,
            schedule.DatasetId,
            schedule.ReportId,
            schedule.PageId,
            schedule.Enabled,
            schedule.Cron,
            schedule.TimeZone,
            schedule.RetryCount,
            schedule.RetryBackoffSeconds,
            schedule.NotifyOnSuccess,
            schedule.NotifyOnFailure,
            schedule.NotifyTargets.Select(t => new RefreshNotificationTargetDto(t.Type, t.Target)).ToList(),
            schedule.CreatedByUserId,
            schedule.CreatedAtUtc,
            schedule.UpdatedAtUtc
        );
    }

    private static DatasetRefreshRunDto ToDto(DatasetRefreshRun run)
    {
        return new DatasetRefreshRunDto(
            run.Id,
            run.ScheduleId,
            run.WorkspaceId,
            run.DatasetId,
            run.ReportId,
            run.PageId,
            run.TriggeredByUserId,
            run.RequestedAtUtc,
            run.StartedAtUtc,
            run.CompletedAtUtc,
            run.Status,
            run.FailureReason,
            run.PowerBiRequestId,
            run.PowerBiActivityId,
            run.RetriesAttempted,
            run.DurationMs
        );
    }

    private static List<RefreshNotificationTarget> MapTargets(List<RefreshNotificationTargetDto>? targets)
    {
        return targets?.Select(t => new RefreshNotificationTarget { Type = t.Type, Target = t.Target }).ToList()
               ?? new List<RefreshNotificationTarget>();
    }
}
