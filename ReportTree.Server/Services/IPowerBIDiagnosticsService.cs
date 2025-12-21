using ReportTree.Server.DTOs;

namespace ReportTree.Server.Services;

public interface IPowerBIDiagnosticsService
{
    Task<PowerBIDiagnosticResultDto> RunAsync(Guid? workspaceId = null, Guid? reportId = null, CancellationToken cancellationToken = default);
}
