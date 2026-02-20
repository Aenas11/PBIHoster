using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.Services;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanManageUsers")]
public class AuditController : ControllerBase
{
    private readonly AuditLogService _auditLogService;

    public AuditController(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 100, [FromQuery] string? actionType = null)
    {
        var logs = await _auditLogService.GetLogsAsync(skip, take, actionType);
        var count = await _auditLogService.GetCountAsync(actionType);
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
}
