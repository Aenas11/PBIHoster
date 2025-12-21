using Microsoft.Extensions.Logging;
using ReportTree.Server.DTOs;

namespace ReportTree.Server.Services;

public class PowerBIDiagnosticsService : IPowerBIDiagnosticsService
{
    private readonly IPowerBIService _powerBIService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PowerBIDiagnosticsService> _logger;

    public PowerBIDiagnosticsService(
        IPowerBIService powerBIService,
        IConfiguration configuration,
        ILogger<PowerBIDiagnosticsService> logger)
    {
        _powerBIService = powerBIService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PowerBIDiagnosticResultDto> RunAsync(
        Guid? workspaceId = null,
        Guid? reportId = null,
        CancellationToken cancellationToken = default)
    {
        var checks = new List<PowerBIDiagnosticCheckDto>();
        var result = new PowerBIDiagnosticResultDto
        {
            AzurePortalLink = BuildAzurePortalLink()
        };

        void AddCheck(string name, DiagnosticStatus status, string detail, string? resolution = null, string? docsUrl = null)
        {
            checks.Add(new PowerBIDiagnosticCheckDto
            {
                Name = name,
                Status = status,
                Detail = detail,
                Resolution = resolution,
                DocsUrl = docsUrl
            });
        }

        var tenantId = _configuration["PowerBI:TenantId"];
        var clientId = _configuration["PowerBI:ClientId"];
        var clientSecret = _configuration["PowerBI:ClientSecret"];
        var authType = _configuration["PowerBI:AuthType"] ?? "ClientSecret";
        var missingConfig = string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId);

        if (missingConfig)
        {
            AddCheck(
                "Configuration",
                DiagnosticStatus.Error,
                "Missing Power BI TenantId or ClientId.",
                "Populate TenantId and ClientId in app settings or environment variables.",
                "https://learn.microsoft.com/power-bi/developer/embedded/embed-service-principal"
            );
            result.Checks = checks;
            return result;
        }

        if (authType.Equals("ClientSecret", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(clientSecret))
        {
            AddCheck(
                "Service principal secret",
                DiagnosticStatus.Warning,
                "Client secret is empty. Authentication will fail unless a certificate is configured.",
                "Add PowerBI:ClientSecret or configure certificate authentication.",
                "https://learn.microsoft.com/entra/identity-platform/howto-create-service-principal-portal"
            );
        }

        try
        {
            await _powerBIService.GetAccessTokenAsync(cancellationToken);
            AddCheck(
                "Access token",
                DiagnosticStatus.Success,
                "Successfully acquired an access token using the configured service principal."
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Power BI token acquisition failed.");
            AddCheck(
                "Access token",
                DiagnosticStatus.Error,
                $"Failed to acquire token: {ex.Message}",
                "Ensure the service principal has API permissions and the secret/certificate is valid.",
                "https://learn.microsoft.com/power-bi/developer/embedded/register-app"
            );
            result.Checks = checks;
            return result;
        }

        IEnumerable<WorkspaceDto> workspaces = Enumerable.Empty<WorkspaceDto>();
        try
        {
            workspaces = await _powerBIService.GetWorkspacesAsync(cancellationToken);
            result.Workspaces = workspaces;
            AddCheck(
                "Workspace listing",
                workspaces.Any() ? DiagnosticStatus.Success : DiagnosticStatus.Warning,
                workspaces.Any()
                    ? $"Retrieved {workspaces.Count()} workspaces."
                    : "No workspaces returned for the service principal.",
                workspaces.Any() ? null : "Grant the service principal access to at least one workspace.",
                "https://learn.microsoft.com/power-bi/enterprise/service-service-principal"
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Workspace lookup failed.");
            AddCheck(
                "Workspace listing",
                DiagnosticStatus.Error,
                $"Unable to list workspaces: {ex.Message}",
                "Ensure the service principal is assigned to the desired workspace (Contributor or Member).",
                "https://learn.microsoft.com/power-bi/enterprise/service-service-principal"
            );
            result.Checks = checks;
            return result;
        }

        WorkspaceDto? targetWorkspace = null;
        if (workspaces.Any())
        {
            targetWorkspace = workspaceId.HasValue
                ? workspaces.FirstOrDefault(w => w.Id == workspaceId)
                : workspaces.FirstOrDefault();

            if (workspaceId.HasValue && targetWorkspace == null)
            {
                AddCheck(
                    "Workspace selection",
                    DiagnosticStatus.Error,
                    $"Workspace {workspaceId} not found for this service principal.",
                    "Confirm the workspace exists and the service principal is a member.",
                    "https://learn.microsoft.com/power-bi/collaborate-share/service-how-to-collaborate-distribute-dashboards-reports#roles"
                );
                result.Checks = checks;
                return result;
            }

            if (targetWorkspace != null)
            {
                result.WorkspaceId = targetWorkspace.Id;
                AddCheck(
                    "Workspace selection",
                    DiagnosticStatus.Success,
                    $"Selected workspace \"{targetWorkspace.Name}\"."
                );
            }
        }

        IEnumerable<ReportDto> reports = Enumerable.Empty<ReportDto>();
        if (targetWorkspace != null)
        {
            try
            {
                reports = await _powerBIService.GetReportsAsync(targetWorkspace.Id, cancellationToken);
                result.Reports = reports;
                AddCheck(
                    "Report listing",
                    reports.Any() ? DiagnosticStatus.Success : DiagnosticStatus.Warning,
                    reports.Any()
                        ? $"Retrieved {reports.Count()} reports in workspace \"{targetWorkspace.Name}\"."
                        : $"No reports found in workspace \"{targetWorkspace.Name}\".",
                    reports.Any() ? null : "Publish or assign a report to the workspace so it can be embedded."
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Report lookup failed.");
                AddCheck(
                    "Report listing",
                    DiagnosticStatus.Error,
                    $"Unable to list reports: {ex.Message}",
                    "Ensure the service principal has Build permission in the workspace.",
                    "https://learn.microsoft.com/power-bi/connect-data/service-dataset-build-permissions"
                );
                result.Checks = checks;
                return result;
            }
        }

        ReportDto? targetReport = null;
        if (reports.Any())
        {
            targetReport = reportId.HasValue
                ? reports.FirstOrDefault(r => r.Id == reportId)
                : reports.FirstOrDefault();

            if (reportId.HasValue && targetReport == null)
            {
                AddCheck(
                    "Report selection",
                    DiagnosticStatus.Error,
                    $"Report {reportId} was not found in workspace \"{targetWorkspace?.Name}\".",
                    "Verify the report exists and the service principal has Build permission."
                );
                result.Checks = checks;
                return result;
            }

            if (targetReport != null)
            {
                result.ReportId = targetReport.Id;
                AddCheck(
                    "Report selection",
                    DiagnosticStatus.Success,
                    $"Selected report \"{targetReport.Name}\"."
                );
            }
        }

        if (targetWorkspace != null && targetReport != null)
        {
            try
            {
                await _powerBIService.GetReportEmbedTokenAsync(targetWorkspace.Id, targetReport.Id, null, cancellationToken);
                AddCheck(
                    "Embed token generation",
                    DiagnosticStatus.Success,
                    "Generated an embed token successfully.",
                    docsUrl: "https://learn.microsoft.com/power-bi/developer/embedded/embed-service-principal"
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Embed token generation failed.");
                AddCheck(
                    "Embed token generation",
                    DiagnosticStatus.Error,
                    $"Failed to generate embed token: {ex.Message}",
                    "Ensure the dataset allows service principal access and the workspace is in a capacity that supports embedding.",
                    "https://learn.microsoft.com/power-bi/developer/embedded/power-bi-embedded-troubleshoot"
                );
            }
        }

        result.Checks = checks;
        result.Success = !checks.Any(c => c.Status == DiagnosticStatus.Error);
        return result;
    }

    private string? BuildAzurePortalLink()
    {
        var clientId = _configuration["PowerBI:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        return $"https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Overview/appId/{clientId}";
    }
}
