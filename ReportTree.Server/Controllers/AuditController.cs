using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.DTOs;
using ReportTree.Server.Models;
using ReportTree.Server.Services;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanManageUsers")]
public class AuditController : ControllerBase
{
    private readonly AuditLogService _auditLogService;
    private readonly AuditExportService _auditExportService;

    public AuditController(AuditLogService auditLogService, AuditExportService auditExportService)
    {
        _auditLogService = auditLogService;
        _auditExportService = auditExportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AuditLogQuery query)
    {
        var validationError = ValidateQueryRange(query.FromUtc, query.ToUtc);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        query.Skip = Math.Max(query.Skip, 0);
        query.Take = query.Take <= 0 ? 100 : Math.Min(query.Take, 500);

        var logs = await _auditLogService.GetLogsAsync(query);
        var count = await _auditLogService.GetCountAsync(query);
        return Ok(new { logs, count });
    }

    [HttpGet("user/{username}")]
    public async Task<IActionResult> GetByUser(string username, [FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var logs = await _auditLogService.GetLogsByUsernameAsync(username, skip, take);
        return Ok(logs);
    }

    [HttpGet("resource/{resource}")]
    public async Task<IActionResult> GetByResource(string resource, [FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var logs = await _auditLogService.GetLogsByResourceAsync(resource, skip, take);
        return Ok(logs);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] AuditExportQueryParameters query)
    {
        var validationError = ValidateQueryRange(query.FromUtc, query.ToUtc);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        if (!string.Equals(query.Format, "csv", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(query.Format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Supported export formats are csv and pdf." });
        }

        var exportQuery = new AuditLogQuery
        {
            Skip = 0,
            Take = 5000,
            Username = query.Username,
            ActionType = query.ActionType,
            Resource = query.Resource,
            FromUtc = query.FromUtc,
            ToUtc = query.ToUtc,
            Success = query.Success
        };

        var logs = (await _auditLogService.GetLogsAsync(exportQuery)).ToList();
        var file = _auditExportService.CreateExport(logs, exportQuery, query.Format);

        await _auditLogService.LogAsync(
            "AUDIT_EXPORT",
            "AuditLog",
            $"format={query.Format.ToLowerInvariant()}; count={logs.Count}; username={query.Username ?? "*"}; actionType={query.ActionType ?? "*"}; resource={query.Resource ?? "*"}; from={query.FromUtc?.ToString("O") ?? "*"}; to={query.ToUtc?.ToString("O") ?? "*"}");

        return File(file.Content, file.ContentType, file.FileName);
    }

    private static string? ValidateQueryRange(DateTime? fromUtc, DateTime? toUtc)
    {
        if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value > toUtc.Value)
        {
            return "fromUtc must be earlier than or equal to toUtc.";
        }

        return null;
    }
}
