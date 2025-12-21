using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.DTOs;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Services;
using System.Security.Claims;

namespace ReportTree.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PowerBIController : ControllerBase
    {
        private readonly IPowerBIService _powerBIService;
        private readonly PageAuthorizationService _pageAuthorizationService;
        private readonly IPageRepository _pageRepository;
        private readonly AuditLogService _auditLogService;
        private readonly ILogger<PowerBIController> _logger;

        public PowerBIController(
            IPowerBIService powerBIService,
            PageAuthorizationService pageAuthorizationService,
            IPageRepository pageRepository,
            AuditLogService auditLogService,
            ILogger<PowerBIController> logger)
        {
            _powerBIService = powerBIService;
            _pageAuthorizationService = pageAuthorizationService;
            _pageRepository = pageRepository;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        [HttpGet("workspaces")]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<ActionResult<IEnumerable<WorkspaceDto>>> GetWorkspaces(CancellationToken cancellationToken)
        {
            var workspaces = await _powerBIService.GetWorkspacesAsync(cancellationToken);
            return Ok(workspaces);
        }

        [HttpGet("workspaces/{workspaceId}/reports")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ReportDto>>> GetReports(Guid workspaceId, CancellationToken cancellationToken)
        {
            var reports = await _powerBIService.GetReportsAsync(workspaceId, cancellationToken);
            return Ok(reports);
        }

        [HttpGet("workspaces/{workspaceId}/dashboards")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<DashboardDto>>> GetDashboards(Guid workspaceId, CancellationToken cancellationToken)
        {
            var dashboards = await _powerBIService.GetDashboardsAsync(workspaceId, cancellationToken);
            return Ok(dashboards);
        }

        [HttpPost("embed/report")]
        [Authorize]
        public async Task<ActionResult<EmbedTokenResponseDto>> GetReportEmbedToken([FromBody] EmbedTokenRequestDto request, CancellationToken cancellationToken)
        {
            if (request.PageId.HasValue)
            {
                var page = await _pageRepository.GetByIdAsync(request.PageId.Value);
                if (page == null)
                {
                    return NotFound("Page not found.");
                }

                if (!_pageAuthorizationService.CanAccessPage(page, User))
                {
                    await _auditLogService.LogAsync("EMBED_REPORT", request.ResourceId.ToString(), $"Access denied for page {request.PageId}", false);
                    return Forbid();
                }
            }
            else if (!User.IsInRole("Admin") && !User.IsInRole("Editor"))
            {
                // If no PageId provided, only Admin/Editor can request tokens
                await _auditLogService.LogAsync("EMBED_REPORT", request.ResourceId.ToString(), "Access denied (no page context)", false);
                return Forbid("PageId is required for non-admin users.");
            }

            // RLS Logic
            List<RLSIdentityDto>? identities = null;
            
            if (request.EnableRLS && request.RLSRoles != null && request.RLSRoles.Any())
            {
                var username = User.Identity?.Name;
                if (!string.IsNullOrEmpty(username))
                {
                    // Fetch the report to get the datasetId for RLS
                    var report = await _powerBIService.GetReportAsync(request.WorkspaceId, request.ResourceId, cancellationToken);
                    if (report != null)
                    {
                        identities = new List<RLSIdentityDto>
                        {
                            new RLSIdentityDto
                            {
                                Username = username,
                                Roles = request.RLSRoles,
                                Datasets = new List<string> { report.DatasetId }
                            }
                        };
                    }
                }
            }
            
            var result = await _powerBIService.GetReportEmbedTokenAsync(request.WorkspaceId, request.ResourceId, identities, cancellationToken);
            var context = request.PageId.HasValue ? $"page {request.PageId}" : "admin preview";
            await _auditLogService.LogAsync("EMBED_REPORT", request.ResourceId.ToString(), $"Embed token generated for {context}");
            return Ok(result);
        }

        [HttpPost("embed/dashboard")]
        [Authorize]
        public async Task<ActionResult<EmbedTokenResponseDto>> GetDashboardEmbedToken([FromBody] EmbedTokenRequestDto request, CancellationToken cancellationToken)
        {
            if (request.PageId.HasValue)
            {
                var page = await _pageRepository.GetByIdAsync(request.PageId.Value);
                if (page == null)
                {
                    return NotFound("Page not found.");
                }

                if (!_pageAuthorizationService.CanAccessPage(page, User))
                {
                    await _auditLogService.LogAsync("EMBED_DASHBOARD", request.ResourceId.ToString(), $"Access denied for page {request.PageId}", false);
                    return Forbid();
                }
                
                var result = await _powerBIService.GetDashboardEmbedTokenAsync(request.WorkspaceId, request.ResourceId, cancellationToken);
                await _auditLogService.LogAsync("EMBED_DASHBOARD", request.ResourceId.ToString(), $"Embed token generated for page {request.PageId}");
                return Ok(result);
            }

            if (User.IsInRole("Admin") || User.IsInRole("Editor"))
            {
                var result = await _powerBIService.GetDashboardEmbedTokenAsync(request.WorkspaceId, request.ResourceId, cancellationToken);
                await _auditLogService.LogAsync("EMBED_DASHBOARD", request.ResourceId.ToString(), "Embed token generated for admin preview");
                return Ok(result);
            }

            await _auditLogService.LogAsync("EMBED_DASHBOARD", request.ResourceId.ToString(), "Access denied (no page context)", false);
            return Forbid("PageId is required for non-admin users.");
        }
    }
}
