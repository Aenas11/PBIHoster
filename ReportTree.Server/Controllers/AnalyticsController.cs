using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.DTOs;
using ReportTree.Server.Services;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly UsageTrackingService _usageTrackingService;

    public AnalyticsController(UsageTrackingService usageTrackingService)
    {
        _usageTrackingService = usageTrackingService;
    }

    [HttpPost("events")]
    [AllowAnonymous]
    public async Task<IActionResult> Ingest([FromBody] UsageEventIngestRequest request)
    {
        if (request.Events == null || request.Events.Count == 0)
        {
            return BadRequest(new { Error = "Events payload is required" });
        }

        var username = User.Identity?.Name ?? "anonymous";
        var accepted = await _usageTrackingService.RecordAsync(request.Events, username);
        return Ok(new { accepted });
    }

    [HttpGet("summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSummary([FromQuery] int days = 30)
    {
        var summary = await _usageTrackingService.GetSummaryAsync(days);
        return Ok(summary);
    }
}